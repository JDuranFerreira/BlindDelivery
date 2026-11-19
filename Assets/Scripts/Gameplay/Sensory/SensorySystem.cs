using BlindDelivery.Core.Balance;
using BlindDelivery.Core.Haptics;
using BlindDelivery.Core.Items;
using BlindDelivery.Core.Movement;
using BlindDelivery.Core.Sensory;
using BlindDelivery.Core.Upgrades;
using BlindDelivery.Gameplay.Items;
using BlindDelivery.Gameplay.Movement;
using UnityEngine;

namespace BlindDelivery.Gameplay.Sensory
{
    /// <summary>
    /// The blind mechanic (Tech Plan §4.4): a cheap, deterministic forward probe on the fixed step,
    /// interpreted by pure Core rules, then published as audio pitch, haptics and — only when the
    /// upgrade is owned — the state machine's slow-down. This component never decides a rule.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SensorySystem : MonoBehaviour
    {
        [SerializeField] SimulationRunner runner;
        [SerializeField] RectLevelGeometry geometry;
        [SerializeField] ItemPlacementController items;
        [SerializeField] EcholocationAudio tone;
        [SerializeField, Min(0.05f)] float refreshSeconds = 0.1f;
        [SerializeField, Min(0.1f)] float warningRangeCells = 3.5f;
        [SerializeField, Min(0.1f)] float slowRangeCells = 3f;
        [SerializeField, Min(0.1f)] float slowDuration = 1.2f;

        IHapticsService haptics;
        IUpgradeService upgrades;
        CoreMotor motor;
        float clock;
        float beaconClock;
        float slowPulseClock;
        bool warningActive;

        public SensoryReading LastReading { get; private set; }

        void OnEnable()
        {
            if (runner == null) runner = FindAnyObjectByType<SimulationRunner>();
            if (geometry == null) geometry = FindAnyObjectByType<RectLevelGeometry>();
            if (items == null) items = FindAnyObjectByType<ItemPlacementController>();
            if (tone == null) tone = FindAnyObjectByType<EcholocationAudio>();
            motor = FindAnyObjectByType<CoreMotor>();

            if (AppBootstrap.Instance != null)
            {
                AppBootstrap.Instance.Services.TryGet<IHapticsService>(out haptics);
                AppBootstrap.Instance.Services.TryGet<IUpgradeService>(out upgrades);
            }

            if (runner != null) runner.Stepped += OnStep;
        }

        void OnDisable()
        {
            if (runner != null) runner.Stepped -= OnStep;
        }

        void OnStep(float deltaTime)
        {
            clock += deltaTime;
            if (clock < refreshSeconds) return;
            clock = 0f;

            if (motor == null || geometry == null || runner == null || runner.Machine == null) return;

            motor.ReadPosition(out float x, out float y);
            int facing = runner.Machine.Facing;
            float ledge = ProbeAhead(x, y - 0.5f, facing, warningRangeCells);
            float wall = ProbeAhead(x, y, facing, warningRangeCells);
            LastReading = SensoryRules.Evaluate(ledge, wall, BeaconDirection(x), warningRangeCells, slowRangeCells);

            tone?.SetWarning(LastReading.WarningIntensity);

            if (LastReading.WarningIntensity > 0f && !warningActive)
            {
                haptics?.Play(HapticPattern.EdgeWarning, LastReading.WarningIntensity);
                warningActive = true;
            }
            else if (LastReading.WarningIntensity <= 0f)
            {
                warningActive = false;
            }

            if (LastReading.HasLedge) haptics?.Play(HapticPattern.LedgeDetected, LastReading.WarningIntensity * 0.5f);

            if (LastReading.BeaconDirection != 0f)
            {
                beaconClock += refreshSeconds;
                if (beaconClock >= 0.5f)
                {
                    beaconClock = 0f;
                    haptics?.Play(
                        LastReading.BeaconDirection > 0f ? HapticPattern.BeaconRight : HapticPattern.BeaconLeft, 0.6f);
                }
            }
            else beaconClock = 0f;

            int level = upgrades != null ? upgrades.Level(UpgradeKind.EcholocationSlowdown) : 0;
            if (level > 0 && LastReading.SlowTrigger && runner.Machine.State == MovementState.Walking)
            {
                runner.Machine.ApplySlow(SensoryRules.SlowFactorForLevel(level), slowDuration);
            }

            if (runner.Machine.State == MovementState.Slowed)
            {
                slowPulseClock += refreshSeconds;
                if (slowPulseClock >= 0.4f)
                {
                    slowPulseClock = 0f;
                    haptics?.Play(HapticPattern.SlowPulse, 0.5f);
                }
            }
            else slowPulseClock = 0f;
        }

        float ProbeAhead(float x, float y, int facing, float rangeCells)
        {
            int cells = Mathf.CeilToInt(rangeCells);
            for (int i = 1; i <= cells; i++)
            {
                if (!geometry.Contains(x + facing * i, y)) return i;
            }

            return 0f;
        }

        float BeaconDirection(float x)
        {
            if (items == null) return 0f;
            foreach (GuidanceItem item in items.PlacedItems)
            {
                if (item.Type == ItemType.AudioBeacon)
                    return item.transform.position.x >= x ? 1f : -1f;
            }

            return 0f;
        }
    }
}
