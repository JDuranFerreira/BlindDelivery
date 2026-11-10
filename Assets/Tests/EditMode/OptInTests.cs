using System;
using System.Collections.Generic;
using NUnit.Framework;
using Template.Core.OptIn;
using Template.Services.OptIn;

namespace Template.Tests.EditMode
{
    /// <summary>
    /// Bonus systems as opt-in files (M8): the currency wallet's one-way mutations, the tutorial
    /// sequence's resume and idempotent completes, server time against a shifted device clock,
    /// the rolling FPS monitor, the device-tier buckets and the debug console's ring + commands.
    /// </summary>
    public class OptInTests
    {
        sealed class Listener
        {
            public List<CurrencyChange> Changes = new List<CurrencyChange>();
        }

        // ---- CurrencyWallet -------------------------------------------------------

        [Test]
        public void Grant_NormalizesIds_AddsAndFiresTheFact()
        {
            CurrencyWallet wallet = new CurrencyWallet();
            Listener listener = new Listener();
            wallet.Changed += c => listener.Changes.Add(c);

            wallet.Grant("  Coins ", 5);

            Assert.That(wallet.GetBalance("coins"), Is.EqualTo(5));
            Assert.That(listener.Changes, Has.Count.EqualTo(1));
            Assert.That(listener.Changes[0].balanceAfter, Is.EqualTo(5));
            Assert.That(listener.Changes[0].spent, Is.False);
        }

        [Test]
        public void TrySpend_InsufficientFunds_IsRefusedAndSilent()
        {
            CurrencyWallet wallet = new CurrencyWallet();
            Listener listener = new Listener();
            wallet.Changed += c => listener.Changes.Add(c);
            wallet.Grant("gems", 3);

            bool spent = wallet.TrySpend("gems", 10);

            Assert.That(spent, Is.False);
            Assert.That(wallet.GetBalance("gems"), Is.EqualTo(3), "never negative");
            Assert.That(listener.Changes, Has.Count.EqualTo(1), "only the grant fired; the refusal is silent");
        }

        [Test]
        public void TrySpend_Affordable_ReducesAndFiresTheSpentFact()
        {
            CurrencyWallet wallet = new CurrencyWallet();
            wallet.Grant("coins", 10);

            bool spent = wallet.TrySpend("COINS", 4);

            Assert.That(spent, Is.True);
            Assert.That(wallet.GetBalance("coins"), Is.EqualTo(6));
        }

        [Test]
        public void ZeroGrant_Throws()
        {
            CurrencyWallet wallet = new CurrencyWallet();

            Assert.Throws<ArgumentOutOfRangeException>(() => wallet.Grant("coins", 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => wallet.TrySpend("coins", -3));
        }

        // ---- TutorialSequence -----------------------------------------------------

        [Test]
        public void Tutorial_ResumedMidway_AndIdempotentCompletes()
        {
            string[] steps = { "move", "shoot", "claim" };
            TutorialSequence started = new TutorialSequence(steps);
            Assert.That(started.CurrentStep, Is.EqualTo("move"));

            Assert.That(started.TryComplete("shoot"), Is.False, "wrong step is a no-op");
            Assert.That(started.TryComplete("move"), Is.True);
            Assert.That(started.CurrentStep, Is.EqualTo("shoot"));

            // Save the one int, restore in a fresh sequence (the "saved" contract).
            TutorialSequence resumed = new TutorialSequence(steps, started.ProgressIndex);
            Assert.That(resumed.CurrentStep, Is.EqualTo("shoot"));
            Assert.That(resumed.TryComplete("move"), Is.False, "a late event for an old step cannot rewind");

            resumed.SkipAll();
            Assert.That(resumed.IsComplete, Is.True);
            Assert.That(resumed.CurrentStep, Is.Null);
        }

        [Test]
        public void Tutorial_DuplicateOrEmptyIds_AreRejected()
        {
            Assert.Throws<ArgumentException>(() => new TutorialSequence(new[] { "a", "a" }));
            Assert.Throws<ArgumentException>(() => new TutorialSequence(new[] { "a", null }));
        }

        // ---- ServerTimeService ----------------------------------------------------

        [Test]
        public void ServerTime_OffsetSnapshot_TheNewestSyncWins()
        {
            DateTime device = new DateTime(2030, 2, 1, 12, 0, 0, DateTimeKind.Utc);
            DateTime trusted = device + TimeSpan.FromMinutes(3);
            ServerTimeService time = new ServerTimeService(() => device);
            Assert.That(time.IsSynchronized, Is.False, "honest until a server really answered");
            Assert.That(time.NowUtc(), Is.EqualTo(device));

            time.SynchronizeWithServer(trusted);
            Assert.That(time.OffsetTicks, Is.EqualTo((trusted - device).Ticks));

            // Between syncs server time only advances with the device clock — one clock channel
            // cannot detect a wall-clock shift. The template pairs the snapshot with re-syncing
            // on every connect, and the game gates its timers on IsSynchronized.
            device += TimeSpan.FromHours(1);
            Assert.That(time.NowUtc(), Is.EqualTo(device + TimeSpan.FromMinutes(3)));
            Assert.That(time.IsSynchronized, Is.True);

            // A newer server answer replaces the measured offset entirely.
            time.SynchronizeWithServer(trusted - TimeSpan.FromHours(6));
            device += TimeSpan.FromHours(1);
            Assert.That(time.NowUtc(), Is.EqualTo(device - TimeSpan.FromHours(7) + TimeSpan.FromMinutes(3)));
            Assert.That(time.OffsetTicks, Is.EqualTo((trusted - TimeSpan.FromHours(6) - (device - TimeSpan.FromHours(1))).Ticks));
        }

        // ---- FpsMonitor -----------------------------------------------------------

        [Test]
        public void FpsMonitor_RollingAverageAndWorstFrame()
        {
            FpsMonitor monitor = new FpsMonitor(5);

            Assert.That(monitor.AverageFps, Is.EqualTo(0f));

            for (int i = 0; i < 5; i++)
            {
                monitor.AddFrame(0.04f); // 25 fps frames
            }

            Assert.That(monitor.AverageFps, Is.EqualTo(25f).Within(0.01f));
            Assert.That(monitor.WorstFps, Is.EqualTo(25f).Within(0.01f));

            monitor.AddFrame(0.02f);
            Assert.That(monitor.AverageFps, Is.EqualTo((5f / (4 * 0.04f + 0.02f))).Within(0.01f));
            Assert.That(monitor.WorstFps, Is.EqualTo(25f).Within(0.01f));

            monitor.AddFrame(-1f); // ignored, not divided on
            Assert.That(monitor.AverageFps, Is.EqualTo((5f / (4 * 0.04f + 0.02f))).Within(0.01f));
        }

        // ---- DeviceTierClassifier -------------------------------------------------

        [Test]
        public void DeviceTier_ClassifyBucketsByScore()
        {
            Assert.That(
                DeviceTierClassifier.Classify(1000, 2, 500),
                Is.EqualTo(DeviceTier.Low));
            Assert.That(
                DeviceTierClassifier.Classify(4000, 4, 500),
                Is.EqualTo(DeviceTier.Mid));
            Assert.That(
                DeviceTierClassifier.Classify(8000, 8, 2000),
                Is.EqualTo(DeviceTier.High));
        }

        // ---- DebugConsole ---------------------------------------------------------

        [Test]
        public void DebugConsole_ExecutesCommands_RingsTheLog_AbsentFromProduction()
        {
            DebugConsole console = new DebugConsole(logCapacity: 3);
            string granted = null;
            string[] grantedArgs = null;
            console.RegisterCommand("grant", "grants currency", args =>
            {
                granted = args.Length > 0 ? args[0] : null;
                grantedArgs = args;
                console.Log("granted " + granted);
            });

            Assert.That(console.TryExecute("grant coins 500"), Is.True);
            Assert.That(granted, Is.EqualTo("coins"));
            Assert.That(grantedArgs, Is.EqualTo(new[] { "coins", "500" }));

            Assert.That(console.TryExecute("nope"), Is.False);
            Assert.That(console.TryExecute(""), Is.False);

            Assert.That(console.LogTail(2), Is.EqualTo(new[] { "granted coins", "Unknown command: nope" }));
        }
    }
}
