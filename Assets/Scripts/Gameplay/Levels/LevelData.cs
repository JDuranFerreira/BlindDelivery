using System;
using System.Collections.Generic;
using BlindDelivery.Core.Items;
using BlindDelivery.Core.Levels;
using BlindDelivery.Gameplay.Items;
using UnityEngine;

namespace BlindDelivery.Gameplay.Levels
{
    /// <summary>
    /// One asset per level: point pool, crate target, prep countdown, walking speed and the
    /// tool set the player gets. The runtime reads the pure <see cref="LevelConfig"/> built by
    /// <see cref="ToLevelConfig"/>, so no gameplay rule depends on this ScriptableObject.
    /// Create via Assets &gt; Create &gt; Blind Delivery &gt; Level Data.
    /// </summary>
    [CreateAssetMenu(menuName = "Blind Delivery/Level Data", fileName = "LevelData")]
    public sealed class LevelData : ScriptableObject
    {
        [SerializeField] string id = "Level01";
        [SerializeField, Min(0)] int startingPoints = 100;
        [SerializeField, Min(1)] int crateTarget = 3;
        [SerializeField, Min(0)] int prepCountdownSeconds = 3;
        [SerializeField, Min(0.1f)] float deliverymanSpeed = 2.5f;
        [SerializeField] ItemDefinitionAsset[] tools = Array.Empty<ItemDefinitionAsset>();
        [SerializeField] ItemDefinitionAsset[] fixtures = Array.Empty<ItemDefinitionAsset>();

        public IReadOnlyList<ItemDefinitionAsset> Tools => tools;

        /// <summary>Level-design-only items (wind zones, etc.). Never enter the factory or HUD.</summary>
        public IReadOnlyList<ItemDefinitionAsset> Fixtures => fixtures;

        public LevelConfig ToLevelConfig()
        {
            var definitions = new List<ItemDefinitionData>();
            foreach (ItemDefinitionAsset tool in tools)
            {
                if (tool != null) definitions.Add(tool.ToData());
            }

            foreach (ItemDefinitionAsset fixture in fixtures)
            {
                if (fixture == null) continue;
                ItemDefinitionData data = fixture.ToData();
                data.Usage = ItemUsage.LevelObject;
                definitions.Add(data);
            }

            return new LevelConfig
            {
                Id = id,
                StartingPoints = startingPoints,
                CrateTarget = crateTarget,
                PrepCountdownSeconds = prepCountdownSeconds,
                DeliverymanSpeed = deliverymanSpeed,
                Tools = definitions,
            };
        }

        /// <summary>Used by authoring/tests to build level data without a serialized scene.</summary>
        public static LevelData Create(
            string levelId, int points, int crates, int prep, float speed, params ItemDefinitionAsset[] grantedTools)
            => Create(levelId, points, crates, prep, speed, grantedTools, Array.Empty<ItemDefinitionAsset>());

        /// <summary>Used by authoring/tests to keep player tools and level fixtures separate.</summary>
        public static LevelData Create(
            string levelId, int points, int crates, int prep, float speed,
            ItemDefinitionAsset[] grantedTools, ItemDefinitionAsset[] levelFixtures)
        {
            LevelData asset = CreateInstance<LevelData>();
            asset.id = levelId;
            asset.startingPoints = points;
            asset.crateTarget = crates;
            asset.prepCountdownSeconds = prep;
            asset.deliverymanSpeed = speed;
            asset.tools = grantedTools ?? Array.Empty<ItemDefinitionAsset>();
            asset.fixtures = levelFixtures ?? Array.Empty<ItemDefinitionAsset>();
            return asset;
        }
    }
}
