using System;
using BlindDelivery.Core.Balance;

namespace BlindDelivery.Core.Items
{
    /// <summary>
    /// Pure rules for guidance-object interactions: the spring impulse and the two overlap
    /// predicates. Kept in Core (no UnityEngine types) so the gameplay maths that decides crate
    /// loss is unit-testable without a scene. Gameplay supplies the bounds as plain floats.
    /// </summary>
    public static class ItemInteractionRules
    {
        /// <summary>Spring impulse: v = sqrt(2 * g * h) for the tuned launch height in cells.</summary>
        public static float LaunchVerticalSpeed(GameBalance balance)
        {
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            return (float)Math.Sqrt(2d * balance.Gravity * balance.JumppadLaunchHeightCells);
        }

        /// <summary>Wind Break: a vertical-only gust of the same tuned 5-cell height.</summary>
        public static float WindLaunchVerticalSpeed(GameBalance balance)
            => LaunchVerticalSpeed(balance);

        /// <summary>Forward impulse as a fraction of walking speed, signed by facing direction.</summary>
        public static float LaunchHorizontalSpeed(GameBalance balance, float facing)
            => LaunchHorizontalSpeed(balance, facing, balance == null ? 0f : balance.DeliverymanSpeed);

        /// <summary>Uses the per-level walking speed when the level supplies one.</summary>
        public static float LaunchHorizontalSpeed(GameBalance balance, float facing, float walkingSpeed)
        {
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            return walkingSpeed * balance.JumppadForwardSpeedFactor * facing;
        }

        /// <summary>Speed multiplier for Bubble Gum (slow) and Oil (speed-up); 1 for other objects.</summary>
        public static float SpeedFactor(GameBalance balance, ItemType type)
        {
            if (balance == null) throw new ArgumentNullException(nameof(balance));
            switch (type)
            {
                case ItemType.BubbleGum: return balance.BubbleGumSpeedFactor;
                case ItemType.Oil: return balance.OilSpeedFactor;
                default: return 1f;
            }
        }

        /// <summary>True when the deliveryman's feet reach the thin spring pad's centre.</summary>
        public static bool ReachedSpringCenter(
            float itemCenterX, float itemMinY, float itemMaxY,
            float feetX, float feetY, float centerReach, float topBand)
        {
            if (Math.Abs(feetX - itemCenterX) > centerReach) return false;
            return feetY <= itemMaxY + topBand && feetY >= itemMinY - topBand * 2f;
        }

        /// <summary>True when the deliveryman's feet are on top of a guidance object.</summary>
        public static bool TouchesTop(
            float itemCenterX, float itemHalfWidth, float itemMaxY,
            float feetX, float feetY, float horizontalReach, float topBand)
        {
            if (Math.Abs(feetX - itemCenterX) > itemHalfWidth + horizontalReach) return false;
            return feetY <= itemMaxY + topBand && feetY >= itemMaxY - topBand * 2f;
        }
    }
}
