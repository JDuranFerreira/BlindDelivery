using System.Reflection;
using UnityEditor;
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
                var flags = BindingFlags.Instance | BindingFlags.NonPublic;
                typeof(CoreBody2D).GetMethod("OnEnable", flags).Invoke(core, null);

                Assert.That(core.Body.isValid, Is.True);
                // The shape may be an explicit thin box (jumppad) rather than the full sprite bounds.
                var expected = (Vector2)instance.transform.position + core.LocalCenter;
                Assert.That(Vector2.Distance(core.Body.position, expected), Is.LessThan(0.001f));

                typeof(CoreBody2D).GetMethod("OnDisable", flags).Invoke(core, null);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void Jumppad_HasThinTriggerBox()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/Sprites/sprite_jumppad.prefab");
            Assert.That(prefab, Is.Not.Null);
            var core = prefab.GetComponent<CoreBody2D>();
            var serialized = new SerializedObject(core);

            Assert.That(serialized.FindProperty("useCustomShape").boolValue, Is.True);
            Assert.That(serialized.FindProperty("customSize").vector2Value.x, Is.EqualTo(1f).Within(0.001f));
            Assert.That(serialized.FindProperty("customSize").vector2Value.y, Is.EqualTo(0.2f).Within(0.001f));
            Assert.That(serialized.FindProperty("isTrigger").boolValue, Is.True);
        }
    }
}
