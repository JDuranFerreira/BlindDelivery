using System.Collections.Generic;
using BlindDelivery.Core.Balance;
using BlindDelivery.Core.Items;
using BlindDelivery.Core.Levels;
using BlindDelivery.Gameplay.Items;
using BlindDelivery.Gameplay.Levels;
using NUnit.Framework;
using UnityEngine;

namespace BlindDelivery.Tests.EditMode
{
    /// <summary>Per-level data must reach pure Core logic without needing a scene.</summary>
    public sealed class LevelConfigTests
    {
        [Test]
        public void LevelConfig_ExposesPerLevelValuesAndGrantedTools()
        {
            var tools = new List<ItemDefinitionData>
            {
                new ItemDefinitionData { Type = ItemType.Spring, MoveCost = 10, CooldownSeconds = 2f },
                new ItemDefinitionData { Type = ItemType.Ramp, MoveCost = 5, CooldownSeconds = 1f },
            };
            var config = new LevelConfig
            {
                Id = "Level01",
                StartingPoints = 120,
                CrateTarget = 4,
                PrepCountdownSeconds = 5,
                DeliverymanSpeed = 3.1f,
                Tools = tools,
            };

            Assert.That(config.Id, Is.EqualTo("Level01"));
            Assert.That(config.StartingPoints, Is.EqualTo(120));
            Assert.That(config.CrateTarget, Is.EqualTo(4));
            Assert.That(config.PrepCountdownSeconds, Is.EqualTo(5));
            Assert.That(config.DeliverymanSpeed, Is.EqualTo(3.1f));
            Assert.That(config.Grants(ItemType.Spring), Is.True);
            Assert.That(config.Grants(ItemType.Cushion), Is.False);
            Assert.That(config.Tools[1].MoveCost, Is.EqualTo(5));
        }

        [Test]
        public void LevelData_ToLevelConfig_CopiesValuesAndAuthoredToolsInOrder()
        {
            ItemDefinitionAsset spring = ItemDefinitionAsset.Create(ItemType.Spring, null, 10, 2f);
            ItemDefinitionAsset ramp = ItemDefinitionAsset.Create(ItemType.Ramp, null, 5, 1f);
            LevelData data = LevelData.Create("Level02", 80, 3, 4, 2.9f, spring, ramp);

            LevelConfig config = data.ToLevelConfig();

            Assert.That(config.Id, Is.EqualTo("Level02"));
            Assert.That(config.StartingPoints, Is.EqualTo(80));
            Assert.That(config.CrateTarget, Is.EqualTo(3));
            Assert.That(config.PrepCountdownSeconds, Is.EqualTo(4));
            Assert.That(config.DeliverymanSpeed, Is.EqualTo(2.9f));
            Assert.That(config.Tools.Count, Is.EqualTo(2));
            Assert.That(config.Tools[0].Type, Is.EqualTo(ItemType.Spring));
            Assert.That(config.Tools[1].Type, Is.EqualTo(ItemType.Ramp));

            Object.DestroyImmediate(spring);
            Object.DestroyImmediate(ramp);
            Object.DestroyImmediate(data);
        }

        [Test]
        public void LevelConfig_SplitsPlayerToolsFromLevelFixtures()
        {
            ItemDefinitionAsset cardbox = ItemDefinitionAsset.Create(
                BlindDelivery.Core.Balance.ItemType.Cushion, ItemUsage.PlayerTool, null, 10, 2f);
            ItemDefinitionAsset wind = ItemDefinitionAsset.Create(
                BlindDelivery.Core.Balance.ItemType.WindBreak, ItemUsage.LevelFixture, null, 8, 1f);
            LevelData data = LevelData.Create("Windy", 100, 3, 3, 2.5f, cardbox, wind);

            LevelConfig config = data.ToLevelConfig();

            Assert.That(config.PlayerTools.Count, Is.EqualTo(1));
            Assert.That(config.PlayerTools[0].Type, Is.EqualTo(BlindDelivery.Core.Balance.ItemType.Cushion));
            Assert.That(config.Fixtures.Count, Is.EqualTo(1));
            Assert.That(config.Fixtures[0].Usage, Is.EqualTo(ItemUsage.LevelFixture));
            Assert.That(config.Grants(BlindDelivery.Core.Balance.ItemType.WindBreak), Is.True);

            Object.DestroyImmediate(cardbox);
            Object.DestroyImmediate(wind);
            Object.DestroyImmediate(data);
        }

        [Test]
        public void LevelData_WithNoTools_ProducesEmptyToolList()
        {
            LevelData data = LevelData.Create("Empty", 100, 3, 3, 2.5f);

            Assert.That(data.ToLevelConfig().Tools, Is.Empty);

            Object.DestroyImmediate(data);
        }
    }
}
