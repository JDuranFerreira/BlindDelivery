using BlindDelivery.Core.Balance;
using BlindDelivery.Core.Items;
using BlindDelivery.Gameplay.Movement;
using UnityEngine;

namespace BlindDelivery.Gameplay.Items
{
    /// <summary>
    /// Deterministic overlap checks between the deliveryman and placed items. Runs from the
    /// fixed-step simulation before the state machine ticks, so a jumppad launch owns the
    /// character's velocity for the whole arc; a cardbox landing is reported when a fall ends.
    /// No physics contact events or legacy 2D APIs are involved.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ItemInteractionSystem : MonoBehaviour
    {
        const float SpringImpulseCooldown = 0.3f;

        [SerializeField] ItemPlacementController items;
        [SerializeField] CoreMotor deliveryman;
        [SerializeField, Min(0.1f)] float halfHeight = 0.5f;
        [SerializeField, Min(0f)] float horizontalReach = 0.6f;
        [SerializeField, Min(0f)] float centerReach = 0.2f;
        [SerializeField, Min(0f)] float topBand = 0.25f;

        GameBalance balance;
        SimulationRunner runner;
        int latchedPadId = -1;
        float springCooldown;

        public void Initialize(GameBalance data, SimulationRunner simulationRunner)
        {
            balance = data;
            runner = simulationRunner;
            // Resolve scene collaborators once, at the composition root, not on every step.
            if (items == null) items = FindAnyObjectByType<ItemPlacementController>();
            if (deliveryman == null) deliveryman = FindAnyObjectByType<CoreMotor>();
        }

        /// <summary>Called by SimulationRunner each fixed step, before the walking controller ticks.</summary>
        public void BeforeTick(float deltaTime, float x, float y)
        {
            if (balance == null || items == null || deliveryman == null || runner == null || runner.Machine == null)
            {
                return;
            }

            springCooldown = Mathf.Max(0f, springCooldown - deltaTime);
            foreach (GuidanceItem item in items.PlacedItems)
            {
                if (!item.IsPlaced || !item.IsSettled || item.Type != ItemType.Spring) continue;

                Bounds pad = item.WorldBounds;
                bool touching = ItemInteractionRules.ReachedSpringCenter(
                    pad.center.x, pad.min.y, pad.max.y, x, y - halfHeight, centerReach, topBand);
                if (item.InstanceId == latchedPadId)
                {
                    // Clear the latch only once the deliveryman steps off this pad.
                    if (!touching) latchedPadId = -1;
                    continue;
                }

                if (!touching || springCooldown > 0f) continue;

                float vertical = ItemInteractionRules.LaunchVerticalSpeed(balance);
                float horizontal = ItemInteractionRules.LaunchHorizontalSpeed(balance, runner.Machine.Facing, runner.LevelSpeed);
                runner.Machine.Launch(deliveryman, horizontal, vertical);
                latchedPadId = item.InstanceId;
                springCooldown = SpringImpulseCooldown;
                break;
            }

            // A cardbox is consumed when the deliveryman reaches its top. This runs on the
            // fixed step regardless of whether the fall was long enough to lose a crate.
            if (IsCushionedLanding(out GuidanceItem cushion))
                cushion.GetComponent<CardboxDestroyEffect>()?.Play();
        }

        /// <summary>True when the deliveryman came down on a cardbox, so the drop costs no crate.</summary>
        public bool IsCushionedLanding() => IsCushionedLanding(out _);

        /// <summary>Also returns the cardbox that absorbed the landing, so it can play its destroy effect.</summary>
        public bool IsCushionedLanding(out GuidanceItem cushion)
        {
            cushion = null;
            if (items == null || deliveryman == null) return false;

            deliveryman.ReadPosition(out float x, out float y);
            foreach (GuidanceItem item in items.PlacedItems)
            {
                if (item.IsPlaced && item.IsSettled && item.Type == ItemType.Cushion)
                {
                    Bounds box = item.WorldBounds;
                    if (ItemInteractionRules.TouchesTop(box.center.x, box.extents.x, box.max.y, x, y - halfHeight, horizontalReach, topBand))
                    {
                        cushion = item;
                        return true;
                    }
                }
            }

            return false;
        }

    }
}
