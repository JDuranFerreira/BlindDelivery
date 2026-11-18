using BlindDelivery.Gameplay.Levels;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BlindDelivery.Tests.EditMode
{
    public class DeliverymanTests
    {
        const string PrefabPath = "Assets/Prefabs/Deliveryman.prefab";
        const string ClipPath = "Assets/Animations/Deliveryman_Walk.anim";
        const string ControllerPath = "Assets/Animations/Deliveryman.controller";

        [Test]
        public void Prefab_HasAnimatorAndFeetPoint()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null, "Deliveryman prefab is missing.");

            var animator = prefab.GetComponent<Animator>();
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
            Assert.That(animator, Is.Not.Null);
            Assert.That(animator.runtimeAnimatorController, Is.EqualTo(controller));
            Assert.That(prefab.GetComponent<SpriteRenderer>(), Is.Not.Null);

            Transform point = prefab.transform.Find("Point");
            Assert.That(point, Is.Not.Null, "Feet Point GameObject is missing.");
            Sprite sprite = prefab.GetComponent<SpriteRenderer>().sprite;
            Assert.That(point.localPosition.y, Is.EqualTo(sprite.bounds.min.y).Within(0.001f));
        }

        [Test]
        public void WalkClip_UsesAllFiveSpritesAndLoops()
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            Assert.That(clip, Is.Not.Null, "Walk clip is missing.");

            var binding = new EditorCurveBinding
            {
                path = "",
                type = typeof(SpriteRenderer),
                propertyName = "m_Sprite",
            };
            ObjectReferenceKeyframe[] curve = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            Assert.That(curve, Is.Not.Null);
            Assert.That(curve.Length, Is.EqualTo(6), "5 frames plus a loop-back keyframe.");
            Assert.That(AnimationUtility.GetAnimationClipSettings(clip).loopTime, Is.True);
        }
    }
}
