using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using Template.Services.Save;
using UnityEngine;

namespace Template.Tests.EditMode
{
    /// <summary>
    /// The disk contract with real files in a temp directory: atomic swap keeps the previous main
    /// as backup, and a main document corrupted on disk still loads from that backup - the
    /// milestone's "kill the app mid-write" acceptance, end to end through the real store.
    /// </summary>
    public class FileSaveStoreTests
    {
        [Serializable]
        sealed class SaveFileDoc
        {
            public float coins;
        }

        string directory;
        FileSaveStore store;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(
                Application.temporaryCachePath,
                "template-save-tests-" + Guid.NewGuid().ToString("N"));
            store = new FileSaveStore(directory);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }

        [Test]
        public async Task WriteThenRead_RoundTrips()
        {
            await store.WriteAsync("progress", "hello");

            Assert.That(await store.ReadMainAsync("progress"), Is.EqualTo("hello"));
            Assert.That(store.Exists("progress"), Is.True);
        }

        [Test]
        public async Task ReplacingWrite_KeepsThePreviousDocumentAsBackup()
        {
            await store.WriteAsync("progress", "one");
            await store.WriteAsync("progress", "two");

            Assert.That(await store.ReadMainAsync("progress"), Is.EqualTo("two"));
            Assert.That(await store.ReadBackupAsync("progress"), Is.EqualTo("one"));
        }

        [Test]
        public async Task MainCorruptedOnDisk_ServiceStillLoadsFromBackup()
        {
            SaveService service = new SaveService(store, new Template.Services.Events.EventBus());

            await service.SaveAsync("progress", new SaveFileDoc
            {
                coins = 7f,
            });
            await service.SaveAsync("progress", new SaveFileDoc
            {
                coins = 9f,
            });

            // Simulate the kill: the main file on disk is now garbage bytes.
            File.WriteAllText(Path.Combine(directory, "progress.json"), "killed-mid-write{");

            SaveFileDoc loaded = await service.LoadAsync<SaveFileDoc>("progress");

            Assert.That(loaded.coins, Is.EqualTo(7f).Within(0.0001f));
        }

        [Test]
        public async Task Backup_AbsentBeforeFirstWrite()
        {
            await store.WriteAsync("progress", "first");

            Assert.That(await store.ReadBackupAsync("progress"), Is.Null);
        }

        [Test]
        public void Keys_AreFileNames_TheStoreRefusesTheRest()
        {
            Assert.Throws<ArgumentException>(() => store.Exists("a/b"));
            Assert.Throws<ArgumentException>(() => store.WriteAsync("a/b", "{}"));
        }
    }
}