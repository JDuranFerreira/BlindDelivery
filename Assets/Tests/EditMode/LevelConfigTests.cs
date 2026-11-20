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
                new ItemDefinitionData { Type = ItemType.JumpPad, MoveCost = 10, CooldownSeconds = 2f },
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
            Assert.That(config.Grants(ItemType.JumpPad), Is.True);
            Assert.That(config.Grants(ItemType.Cardbox), Is.False);
            Assert.That(config.Tools[1].MoveCost, Is.EqualTo(5));
        }

        [Test]
        public void LevelData_ToLevelConfig_CopiesValuesAndAuthoredToolsInOrder()
        {
            ItemDefinitionAsset spring = ItemDefinitionAsset.Create(ItemType.JumpPad, null, 10, 2f);
            ItemDefinitionAsset ramp = ItemDefinitionAsset.Create(ItemType.Ramp, null, 5, 1f);
            LevelData data = LevelData.Create("Level02", 80, 3, 4, 2.9f, spring, ramp);

            LevelConfig config = data.ToLevelConfig();

            Assert.That(config.Id, Is.EqualTo("Level02"));
            Assert.That(config.StartingPoints, Is.EqualTo(80));
            Assert.That(config.CrateTarget, Is.EqualTo(3));
            Assert.That(config.PrepCountdownSeconds, Is.EqualTo(4));
            Assert.That(config.DeliverymanSpeed, Is.EqualTo(2.9f));
            Assert.That(config.Tools.Count, Is.EqualTo(2));
            Assert.That(config.Tools[0].Type, Is.EqualTo(ItemType.JumpPad));
            Assert.That(config.Tools[1].Type, Is.EqualTo(ItemType.Ramp));

            Object.DestroyImmediate(spring);
            Object.DestroyImmediate(ramp);
            Object.DestroyImmediate(data);
        }

        [Test]
        public void LevelConfig_SplitsPlayerToolsFromLevelFixtures()
        {
            ItemDefinitionAsset cardbox = ItemDefinitionAsset.Create(
                BlindDelivery.Core.Balance.ItemType.Cardbox, ItemUsage.Guidance, null, 10, 2f);
            ItemDefinitionAsset wind = ItemDefinitionAsset.Create(
                BlindDelivery.Core.Balance.ItemType.WindBreak, ItemUsage.LevelObject, null, 8, 1f);
            LevelData data = LevelData.Create("Windy", 100, 3, 3, 2.5f, cardbox, wind);

            LevelConfig config = data.ToLevelConfig();

            Assert.That(config.GuidanceObjects.Count, Is.EqualTo(1));
            Assert.That(config.GuidanceObjects[0].Type, Is.EqualTo(BlindDelivery.Core.Balance.ItemType.Cardbox));
            Assert.That(config.LevelObjects.Count, Is.EqualTo(1));
            Assert.That(config.LevelObjects[0].Usage, Is.EqualTo(ItemUsage.LevelObject));
            Assert.That(config.Grants(BlindDelivery.Core.Balance.ItemType.WindBreak), Is.True);

            Object.DestroyImmediate(cardbox);
            Object.DestroyImmediate(wind);
            Object.DestroyImmediate(data);
        }

        [Test]
        public void LevelData_Fixtures_AreSeparateFromPlayerTools()
        {
            ItemDefinitionAsset bridge = ItemDefinitionAsset.Create(ItemType.Ramp, null, 5, 1f);
            ItemDefinitionAsset wind = ItemDefinitionAsset.Create(
                ItemType.WindBreak, ItemUsage.LevelObject, null, 8, 1f);
            LevelData data = LevelData.Create(
                "Windy", 100, 3, 3, 2.5f, new[] { bridge }, new[] { wind });

            LevelConfig config = data.ToLevelConfig();

            Assert.That(config.Tools.Count, Is.EqualTo(2));
            Assert.That(config.GuidanceObjects.Count, Is.EqualTo(1));
            Assert.That(config.GuidanceObjects[0].Type, Is.EqualTo(ItemType.Ramp));
            Assert.That(config.LevelObjects.Count, Is.EqualTo(1));
            Assert.That(config.LevelObjects[0].Type, Is.EqualTo(ItemType.WindBreak));

            Object.DestroyImmediate(bridge);
            Object.DestroyImmediate(wind);
            Object.DestroyImmediate(data);
        }

        [Test]
        public void LevelData_FixturesList_AlwaysMarksEntriesAsLevelFixtures()
        {
            ItemDefinitionAsset misplaced = ItemDefinitionAsset.Create(ItemType.Cardbox, null, 10, 2f);
            LevelData data = LevelData.Create(
                "Strict", 100, 3, 3, 2.5f, System.Array.Empty<ItemDefinitionAsset>(), new[] { misplaced });

            LevelConfig config = data.ToLevelConfig();

            Assert.That(config.GuidanceObjects, Is.Empty);
            Assert.That(config.LevelObjects.Count, Is.EqualTo(1));
            Assert.That(config.LevelObjects[0].Usage, Is.EqualTo(ItemUsage.LevelObject));

            Object.DestroyImmediate(misplaced);
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
