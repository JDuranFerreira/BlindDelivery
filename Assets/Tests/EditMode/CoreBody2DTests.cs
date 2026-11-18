using System.Reflection;
using BlindDelivery.Gameplay.Physics;
using NUnit.Framework;
using UnityEngine;

namespace BlindDelivery.Tests.EditMode
{
    public class CoreBody2DTests
    {
        [Test]
        public void BodyIsCenteredOnSpriteBounds_WithBottomCenterPivot()
        {
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Sprites/sprite_jumppad.prefab");
            Assert.That(prefab, Is.Not.Null, "Bottom-centre sprite prefab is missing.");

            GameObject instance = Object.Instantiate(prefab);
            try
            {
                instance.transform.position = new Vector3(1f, 0f, 0f);
                var core = instance.GetComponent<CoreBody2D>();
                var renderer = instance.GetComponent<SpriteRenderer>();
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(CoreBody2D).GetMethod("OnEnable", flags).Invoke(core, null);

                Assert.That(core.Body.isValid, Is.True);
                Assert.That(Vector2.Distance(core.Body.position, renderer.bounds.center), Is.LessThan(0.001f));

                typeof(CoreBody2D).GetMethod("OnDisable", flags).Invoke(core, null);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }
    }
}
