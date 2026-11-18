using BlindDelivery.Gameplay.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BlindDelivery.Tests.EditMode
{
    public class LevelTests
    {
        const string LevelPrefabPath = "Assets/Prefabs/Levels/Level.prefab";

        GameObject instance;

        [TearDown]
        public void TearDown()
        {
            if (instance != null)
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void LevelPrefab_HasStartAndEndMarkers()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LevelPrefabPath);
            Assert.That(prefab, Is.Not.Null, "Level prefab is missing.");

            Assert.That(prefab.transform.Find("StartPoint"), Is.Not.Null, "StartPoint GameObject is missing.");
            Assert.That(prefab.transform.Find("EndPoint"), Is.Not.Null, "EndPoint GameObject is missing.");
        }

        [Test]
        public void SpawnDeliveryman_PlacesCharacterOnStartPoint()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LevelPrefabPath);
            instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Level level = instance.GetComponent<Level>();

            GameObject deliveryman = level.SpawnDeliveryman();

            Assert.That(deliveryman, Is.Not.Null);
            Assert.That(deliveryman.GetComponent<Deliveryman>(), Is.Not.Null);
            Assert.That(deliveryman.transform.position, Is.EqualTo(level.StartPoint.position));
            Assert.That(deliveryman.transform.parent, Is.EqualTo(instance.transform));
        }

        [Test]
        public void Level01_SpawnsDeliverymanOnStartPoint()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Levels/Level01.prefab");
            Assert.That(prefab, Is.Not.Null, "Level01 prefab is missing.");
            instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Level level = instance.GetComponent<Level>();

            GameObject deliveryman = level.SpawnDeliveryman();

            Assert.That(deliveryman, Is.Not.Null);
            Assert.That(deliveryman.transform.position, Is.EqualTo(level.StartPoint.position));
        }
    }
}
