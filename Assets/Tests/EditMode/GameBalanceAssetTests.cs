using NUnit.Framework;
using UnityEditor;
using BlindDelivery.Core.Balance;
using BlindDelivery.Gameplay.Balance;

namespace BlindDelivery.Tests.EditMode
{
    /// <summary>
    /// Phase 0 done-when: the shipped GameBalance asset provides Safe Fall Height 3.
    /// </summary>
    public class GameBalanceAssetTests
    {
        const string BalanceAssetPath = "Assets/Data/GameBalance.asset";

        [Test]
        public void GameBalanceAsset_Exists()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<GameBalanceAsset>(BalanceAssetPath), Is.Not.Null,
                "Run Blind Delivery > Setup > Phase 0 Foundation to create it.");
        }

        [Test]
        public void GameBalanceAsset_ReadsSafeFallHeightThree()
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameBalanceAsset>(BalanceAssetPath);
            Assert.That(asset, Is.Not.Null);

            GameBalance balance = asset.ToGameBalance();
            Assert.That(balance.SafeFallHeightCells, Is.EqualTo(3));
        }

        [Test]
        public void GameBalanceAsset_CarriesEveryItemTuning()
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameBalanceAsset>(BalanceAssetPath);
            Assert.That(asset, Is.Not.Null);

            GameBalance balance = asset.ToGameBalance();
            foreach (ItemType type in (ItemType[])System.Enum.GetValues(typeof(ItemType)))
            {
                Assert.That(balance.ItemMoveCosts.ContainsKey(type), Is.True, $"Missing move cost for {type}");
                Assert.That(balance.ItemCooldownSeconds.ContainsKey(type), Is.True, $"Missing cooldown for {type}");
            }
        }
    }
}
