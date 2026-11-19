using BlindDelivery.Core.Balance;
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
        }

        /// <summary>Called by SimulationRunner each fixed step, before the walking controller ticks.</summary>
        public void BeforeTick(float deltaTime, float x, float y)
        {
            if (items == null) items = FindAnyObjectByType<ItemPlacementController>();
            if (deliveryman == null) deliveryman = FindAnyObjectByType<CoreMotor>();
            if (runner == null) runner = FindAnyObjectByType<SimulationRunner>();
            if (balance == null || items == null || deliveryman == null || runner == null || runner.Machine == null)
            {
                return;
            }

            springCooldown = Mathf.Max(0f, springCooldown - deltaTime);
            foreach (GuidanceItem item in items.PlacedItems)
            {
                if (!item.IsPlaced || !item.IsSettled || item.Type != ItemType.Spring) continue;

                bool touching = ReachedSpringCenter(item, x, y);
                if (item.InstanceId == latchedPadId)
                {
                    // Clear the latch only once the deliveryman steps off this pad.
                    if (!touching) latchedPadId = -1;
                    continue;
                }

                if (!touching || springCooldown > 0f) continue;

                float height = balance.JumppadLaunchHeightCells; // one cell == one world unit
                float vertical = Mathf.Sqrt(2f * balance.Gravity * height);
                float horizontal = balance.DeliverymanSpeed * balance.JumppadForwardSpeedFactor * runner.Machine.Facing;
                runner.Machine.Launch(deliveryman, horizontal, vertical);
                latchedPadId = item.InstanceId;
                springCooldown = SpringImpulseCooldown;
                break;
            }
        }

        /// <summary>True when the deliveryman came down on a cardbox, so the drop costs no crate.</summary>
        public bool IsCushionedLanding()
        {
            if (items == null) items = FindAnyObjectByType<ItemPlacementController>();
            if (deliveryman == null) deliveryman = FindAnyObjectByType<CoreMotor>();
            if (items == null || deliveryman == null) return false;

            deliveryman.ReadPosition(out float x, out float y);
            foreach (GuidanceItem item in items.PlacedItems)
            {
                if (item.IsPlaced && item.IsSettled && item.Type == ItemType.Cushion && TouchesTop(item, x, y)) return true;
            }

            return false;
        }

        /// <summary>The thin pad does not block walking; it fires when he reaches its centre.</summary>
        bool ReachedSpringCenter(GuidanceItem item, float x, float y)
        {
            Bounds bounds = item.WorldBounds;
            if (Mathf.Abs(x - bounds.center.x) > centerReach) return false;

            float feetY = y - halfHeight;
            return feetY <= bounds.max.y + topBand && feetY >= bounds.min.y - topBand * 2f;
        }

        bool TouchesTop(GuidanceItem item, float x, float y)
        {
            Bounds bounds = item.WorldBounds;
            if (Mathf.Abs(x - bounds.center.x) > bounds.extents.x + horizontalReach) return false;

            float feetY = y - halfHeight;
            return feetY <= bounds.max.y + topBand && feetY >= bounds.max.y - topBand * 2f;
        }
    }
}
