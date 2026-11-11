using System;
using NUnit.Framework;
using BlindDelivery.Core.Purchases;
using BlindDelivery.Core.Save;
using BlindDelivery.Services.Events;
using BlindDelivery.Services.Purchases;
using BlindDelivery.Services.Save;
using BlindDelivery.Tests.EditMode.Fakes;

namespace BlindDelivery.Tests.EditMode
{
    /// <summary>
    /// The M7 acceptance gates for purchases: unknown product refused, the same non-consumable
    /// cannot be granted twice, a purchase survives a simulated crash (a fresh service over the
    /// same store sees the ownership), restore re-persists non-consumables without re-firing
    /// grants, and a failed purchase grants and fires nothing.
    /// </summary>
    public class PurchaseServiceTests
    {
        static readonly ProductData Coins = new ProductData("coins_small", ProductType.Consumable);
        static readonly ProductData NoAds = new ProductData("no_ads", ProductType.NonConsumable);
        static readonly ProductData Vip = new ProductData("vip_month", ProductType.Subscription);

        StubStoreSink store;
        SaveService save;
        EventBus bus;
        int completedEvents;

        [SetUp]
        public void SetUp()
        {
            store = new StubStoreSink();
            bus = new EventBus();
            save = new SaveService(new InMemorySaveStore(), bus);
            completedEvents = 0;
        }

        PurchaseService Build(ProductData[] products = null, InMemorySaveStore backing = null)
        {
            PurchaseService service = new PurchaseService(
                store,
                new FakePurchaseCatalog(products ?? new[] { Coins, NoAds, Vip }),
                SaveOver(backing));
            service.InitializeAsync().Wait();
            service.OnPurchaseCompleted += _ => completedEvents++;
            return service;
        }

        ISaveService SaveOver(InMemorySaveStore backing)
        {
            return backing == null ? save : new SaveService(backing, bus);
        }

        [Test]
        public void UnknownProduct_IsRefused_BeforeTheStoreIsAsked()
        {
            PurchaseService service = Build();

            PurchaseResult result = service.PurchaseAsync("nope").Result;

            Assert.That(result.Success, Is.False);
            Assert.That(result.FailureReason, Is.EqualTo("UnknownProduct"));
            Assert.That(store.LastProductId, Is.Null, "the catalog gates the store, not the other way round");
            Assert.That(completedEvents, Is.EqualTo(0));
        }

        [Test]
        public void NonConsumable_CannotBeGrantedTwice()
        {
            PurchaseService service = Build();

            Assert.That(service.PurchaseAsync("no_ads").Result.Success, Is.True);
            PurchaseResult second = service.PurchaseAsync("no_ads").Result;

            Assert.That(second.Success, Is.False);
            Assert.That(second.FailureReason, Is.EqualTo("AlreadyOwned"));
            Assert.That(completedEvents, Is.EqualTo(1), "exactly one completed grant, ever");
            Assert.That(service.GetOwnedProducts(), Is.EqualTo(new[] { "no_ads" }));
        }

        [Test]
        public void Consumable_IsRebuyable_AndNeverEntersOwned()
        {
            PurchaseService service = Build();

            Assert.That(service.PurchaseAsync("coins_small").Result.Success, Is.True);
            Assert.That(service.PurchaseAsync("coins_small").Result.Success, Is.True);

            Assert.That(service.IsOwned("coins_small"), Is.False);
            Assert.That(completedEvents, Is.EqualTo(2));
            Assert.That(service.GetOwnedProducts(), Is.Empty);
        }

        [Test]
        public void SavesAreNotWrittenForConsumables()
        {
            Build().PurchaseAsync("coins_small").Wait();

            Assert.That(save.Exists("purchases"), Is.False, "session-only kinds stay out of the durable document");
        }

        [Test]
        public void Purchase_SurvivesSimulatedCrash()
        {
            InMemorySaveStore backing = new InMemorySaveStore();
            PurchaseService victim = Build(null, backing);
            Assert.That(victim.PurchaseAsync("no_ads").Result.Success, Is.True);

            // A brand-new service over the same store, nothing else alive.
            PurchaseService reborn = new PurchaseService(
                new StubStoreSink(),
                new FakePurchaseCatalog(new[] { Coins, NoAds, Vip }),
                new SaveService(backing, bus));
            reborn.InitializeAsync().Wait();

            Assert.That(reborn.IsOwned("no_ads"), Is.True, "a paid unlock must not die with the process");
            Assert.That(reborn.GetOwnedProducts(), Is.EqualTo(new[] { "no_ads" }));
        }

        [Test]
        public void Restore_PersistsNonConsumables_Only_AndFiresNoGrant()
        {
            InMemorySaveStore backing = new InMemorySaveStore();
            store.WithRestoreOwned(new[] { "no_ads", "coins_small", "store_only" });
            PurchaseService service = Build(null, backing);

            System.Collections.Generic.IReadOnlyCollection<string> restored = service.RestorePurchasesAsync().Result;

            Assert.That(restored, Is.EqualTo(new object[] { "no_ads", "coins_small", "store_only" }), "the store's owned ids come back untouched");
            Assert.That(service.IsOwned("no_ads"), Is.True, "non-consumables land in ownership");
            Assert.That(service.IsOwned("coins_small"), Is.False, "consumables are not restored into ownership");
            Assert.That(service.IsOwned("store_only"), Is.False, "ids unknown to the catalog grant nothing");
            Assert.That(completedEvents, Is.EqualTo(0), "restore must not re-run grant effects");

            SaveService reopened = new SaveService(backing, bus);
            PurchaseService reborn = new PurchaseService(new StubStoreSink(), new FakePurchaseCatalog(new[] { Coins, NoAds, Vip }), reopened);
            reborn.InitializeAsync().Wait();
            Assert.That(reborn.IsOwned("no_ads"), Is.True, "restore persists exactly like a purchase");
        }

        [Test]
        public void FailedPurchase_GrantsNothing_AndFiresNothing()
        {
            store.WithOutcome(_ => false);
            PurchaseService service = Build();

            PurchaseResult result = service.PurchaseAsync("no_ads").Result;

            Assert.That(result.Success, Is.False);
            Assert.That(result.FailureReason, Is.EqualTo("RejectedOrCancelled"));
            Assert.That(service.IsOwned("no_ads"), Is.False);
            Assert.That(completedEvents, Is.EqualTo(0));
        }
    }
}
