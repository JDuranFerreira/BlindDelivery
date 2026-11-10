using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Template.Core.Events;
using Template.Core.Save;
using Template.Services.Events;
using Template.Services.Save;
using Template.Tests.EditMode.Fakes;
using UnityEngine;

namespace Template.Tests.EditMode
{
    /// <summary>The one save direction worth pinning: a payload survives every hop it can take on disk.</summary>
    [Serializable]
    public sealed class SaveDocument
    {
        public float coins;
        public bool unlocked;
    }

    [Serializable]
    public sealed class DocumentV1
    {
        public float coins;
    }

    [Serializable]
    public sealed class DocumentV2
    {
        public float score;
    }

    [Serializable]
    public sealed class DocumentV3
    {
        public float score;
        public int stars;
    }

    /// <summary>A migration step as tests would ship one: pure text surgery over the payload JSON.</summary>
    sealed class FakeMigration : ISaveMigration
    {
        readonly Func<string, string> migrate;

        public FakeMigration(int from, int to, Func<string, string> migrate)
        {
            FromVersion = from;
            ToVersion = to;
            this.migrate = migrate;
        }

        public int FromVersion { get; }

        public int ToVersion { get; }

        public string Migrate(string payloadJson) => migrate(payloadJson);
    }

    /// <summary>
    /// Save logic against the memory store: the migration chain (the milestone's named acceptance),
    /// fresh defaults, forward-only versions, and the corruption ladder - main alone, then backup,
    /// then both failing.
    /// </summary>
    public class SaveServiceTests
    {
        InMemorySaveStore store;
        EventBus bus;
        SaveService service;
        List<string> written;

        [SetUp]
        public void SetUp()
        {
            store = new InMemorySaveStore();
            bus = new EventBus();
            service = new SaveService(store, bus);
            written = new List<string>();
            bus.Subscribe<SaveWritten>(e => written.Add($"{e.Key}@{e.Version}"));
        }

        static string Envelope(int version, string payloadJson)
        {
            return JsonUtility.ToJson(new EnvelopeShim
            {
                version = version,
                payload = payloadJson,
            });
        }

        [Serializable]
        class EnvelopeShim
        {
            public int version;
            public string payload;
        }

        [Test]
        public async Task SaveThenLoad_RoundTrips_AndPublishesWritten()
        {
            await service.SaveAsync("progress", new SaveDocument
            {
                coins = 7f,
                unlocked = true,
            });

            SaveDocument loaded = await service.LoadAsync<SaveDocument>("progress");

            Assert.That(loaded.coins, Is.EqualTo(7f).Within(0.0001f));
            Assert.That(loaded.unlocked, Is.True);
            Assert.That(service.Exists("progress"), Is.True);
            Assert.That(service.GetStoredVersion("progress"), Is.EqualTo(1));
            Assert.That(written, Is.EqualTo(new[] { "progress@1" }));
        }

        [Test]
        public async Task NeverSaved_LoadsFreshDefaults()
        {
            SaveDocument loaded = await service.LoadAsync<SaveDocument>("progress");

            Assert.That(loaded.coins, Is.EqualTo(0f));
            Assert.That(loaded.unlocked, Is.False);
            Assert.That(service.Exists("progress"), Is.False);
            Assert.That(service.GetStoredVersion("progress"), Is.EqualTo(-1));
        }

        [Test]
        public async Task MigrationChain_PullsOlderSavesForward()
        {
            service.RegisterMigration(new FakeMigration(1, 2, json =>
                JsonUtility.ToJson(new DocumentV2
                {
                    score = JsonUtility.FromJson<DocumentV1>(json).coins,
                })));
            service.RegisterMigration(new FakeMigration(2, 3, json =>
                JsonUtility.ToJson(new DocumentV3
                {
                    score = JsonUtility.FromJson<DocumentV2>(json).score,
                    stars = 3,
                })));

            Assert.That(service.CurrentVersion, Is.EqualTo(3));

            // A save produced by game version one of the schema sits in the store untouched.
            store.OverwriteMain("progress", Envelope(1, JsonUtility.ToJson(new DocumentV1
            {
                coins = 7f,
            })));

            DocumentV3 loaded = await service.LoadAsync<DocumentV3>("progress");

            Assert.That(loaded.score, Is.EqualTo(7f).Within(0.0001f));
            Assert.That(loaded.stars, Is.EqualTo(3));
            // The stored version is what the document carried, not what the chain migrated it to.
            Assert.That(service.GetStoredVersion("progress"), Is.EqualTo(1));
            Assert.That(written, Is.Empty);
        }

        [Test]
        public void MigrationChain_MustStartAtOne_AndStayContiguous()
        {
            Assert.Throws<InvalidOperationException>(
                () => service.RegisterMigration(new FakeMigration(2, 3, s => s)));
            Assert.Throws<InvalidOperationException>(
                () => service.RegisterMigration(new FakeMigration(1, 3, s => s)));

            service.RegisterMigration(new FakeMigration(1, 2, s => s));
            Assert.Throws<InvalidOperationException>(
                () => service.RegisterMigration(new FakeMigration(3, 4, s => s)));
            Assert.Throws<InvalidOperationException>(
                () => service.RegisterMigration(new FakeMigration(1, 2, s => s)));
        }

        [Test]
        public async Task SaveFromANewerBuild_ThrowsInsteadOfMisreading()
        {
            store.OverwriteMain("progress", Envelope(9, "{}"));

            Assert.ThrowsAsync<SaveVersionException>(
                () => service.LoadAsync<SaveDocument>("progress"));
            Assert.That(service.GetStoredVersion("progress"), Is.EqualTo(-1));
        }

        [Test]
        public async Task CorruptMain_WithNoBackup_ThrowsCorrupt()
        {
            await service.SaveAsync("progress", new SaveDocument
            {
                coins = 7f,
            });
            store.OverwriteMain("progress", "not{json");
            store.OverwriteBackup("progress", "not{json");

            Assert.ThrowsAsync<SaveCorruptException>(
                () => service.LoadAsync<SaveDocument>("progress"));
        }

        [Test]
        public async Task CorruptMainDocument_LoadsTheBackup()
        {
            await service.SaveAsync("progress", new SaveDocument
            {
                coins = 7f,
            });
            await service.SaveAsync("progress", new SaveDocument
            {
                coins = 9f,
            });

            // The main document died mid-life: the previous write survives as the backup.
            store.OverwriteMain("progress", "garbage{");

            SaveDocument loaded = await service.LoadAsync<SaveDocument>("progress");

            Assert.That(loaded.coins, Is.EqualTo(7f).Within(0.0001f));
        }

        [Test]
        public async Task MainDocumentLostEntirely_BackupStillLoads()
        {
            await service.SaveAsync("progress", new SaveDocument
            {
                coins = 4f,
            });
            await service.SaveAsync("progress", new SaveDocument
            {
                coins = 9f,
            });
            // A kill between the delete and the move of the swap leaves exactly this state: no
            // main document, the previous generation safe in the backup.
            store.DropMain("progress");

            SaveDocument loaded = await service.LoadAsync<SaveDocument>("progress");

            Assert.That(loaded.coins, Is.EqualTo(4f).Within(0.0001f));
        }

        [Test]
        public void Keys_BecomeFileNames_PathPunctuationIsRefused()
        {
            Assert.ThrowsAsync<ArgumentException>(
                () => service.SaveAsync<SaveDocument>("bad/key", new SaveDocument()));
            Assert.Throws<ArgumentException>(() => service.Exists("bad key"));
        }
    }
}