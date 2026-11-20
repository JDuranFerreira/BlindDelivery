using System;
using BlindDelivery.Core.Balance;
using BlindDelivery.Core.Crates;
using BlindDelivery.Core.Levels;
using BlindDelivery.Core.Movement;
using BlindDelivery.Core.Scoring;
using BlindDelivery.Gameplay.Balance;
using BlindDelivery.Gameplay.Items;
using BlindDelivery.Gameplay.Levels;
using Unity.U2D.Physics;
using UnityEngine;

namespace BlindDelivery.Gameplay.Movement
{
    /// <summary>
    /// Owns simulation time. Every fixed step it scans, ticks the pure movement state machine,
    /// then advances the 2D Physics Core world manually. Systems are added as separate steps
    /// later (cooldowns, wind, hazards, delivery events) without changing the state machine.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public sealed class SimulationRunner : MonoBehaviour
    {
        [SerializeField] GameBalanceAsset balance;
        [SerializeField] Level level;
        [SerializeField] CoreMotor deliverymanMotor;
        [SerializeField] ProbeMovementSensor sensor;
        [SerializeField] RectLevelGeometry geometry;
        [SerializeField] ItemInteractionSystem interactions;
        [SerializeField] ItemPlacementController itemController;
        [SerializeField, Min(0.1f)] float maxCatchUpSeconds = 0.25f;
        [SerializeField, Min(0.05f)] float deliveryRadius = 1.5f;
        [SerializeField, Min(0f)] float fallOutMargin = 1f;

        DeliverymanStateMachine machine;
        PhysicsWorld world;
        PhysicsWorld.SimulationType previousSimulationType;
        float fixedStep;
        float accumulator;
        bool worldReady;
        int safeFallHeightCells;
        float prepRemaining;
        float prepDuration;
        bool runEnded;
        bool timerStarted;
        readonly RunTimer timer = new RunTimer();

        /// <summary>Raised once when the courier reaches point B with at least one crate.</summary>
        public event Action RunDelivered;

        /// <summary>Raised once when the last crate is lost.</summary>
        public event Action RunFailed;

        /// <summary>Raised when a crate is actually lost, with the courier's position.</summary>
        public event Action<Vector2> CrateLost;

        /// <summary>Raised each active fixed step before the movement state machine ticks.</summary>
        public event Action<float> Stepped;

        public DeliverymanStateMachine Machine => machine;

        /// <summary>True while the prep countdown is running; the courier waits at A and the player may place items.</summary>
        public bool IsPreparing => prepRemaining > 0f;

        public float PrepRemaining => prepRemaining;

        public float PrepDuration => prepDuration;

        /// <summary>Elapsed gameplay time, shown on the result screen (GDD §5).</summary>
        public float RunSeconds => timer.ElapsedSeconds;

        /// <summary>Walking speed from this level's data (or GameBalance on legacy scenes).</summary>
        public float LevelSpeed { get; private set; }

        /// <summary>Crate state for this run; item interactions consume it on unsafe landings.</summary>
        public CrateStack Crates { get; private set; }

        /// <summary>Hazard rule (GDD §4): lose one crate without waiting for a landing.</summary>
        public void LoseCrateFromHazard()
        {
            if (!runEnded) OnFallLanded(safeFallHeightCells + 1f);
        }

        /// <summary>Hazard rule (GDD §4): a lethal object ends the run immediately.</summary>
        public void FailRun() => EndRun(delivered: false);

        void OnEnable()
        {
            TryInitialize();
        }

        /// <summary>
        /// The Deliveryman is spawned by Level.Awake, so the motor may not exist on the runner's
        /// first OnEnable. Retrying keeps the runner independent of Awake ordering.
        /// </summary>
        bool TryInitialize()
        {
            if (balance == null) balance = FindAnyObjectByType<GameBalanceAsset>();
            if (level == null) level = FindAnyObjectByType<Level>();
            if (deliverymanMotor == null) deliverymanMotor = FindAnyObjectByType<CoreMotor>();
            if (sensor == null) sensor = FindAnyObjectByType<ProbeMovementSensor>();
            if (geometry == null) geometry = FindAnyObjectByType<RectLevelGeometry>();
            if (balance == null || level == null || deliverymanMotor == null || sensor == null || geometry == null)
            {
                return false;
            }

            GameBalance data = balance.ToGameBalance();
            LevelConfig levelConfig = level != null && level.Data != null ? level.Data.ToLevelConfig() : null;
            LevelSpeed = levelConfig != null ? levelConfig.DeliverymanSpeed : data.DeliverymanSpeed;
            fixedStep = data.FixedStepSeconds;
            safeFallHeightCells = data.SafeFallHeightCells;
            // Legacy scenes without LevelData keep the old immediate-walk behaviour.
            prepDuration = levelConfig != null ? levelConfig.PrepCountdownSeconds : 0f;
            prepRemaining = prepDuration;
            runEnded = false;
            timerStarted = false;
            timer.Stop();
            geometry.Rebuild();
            machine = new DeliverymanStateMachine(LevelSpeed);
            machine.FallLanded += OnFallLanded;
            int crateCount = data.BaseCrateCount;
            if (BlindDelivery.Gameplay.AppBootstrap.Instance != null &&
                BlindDelivery.Gameplay.AppBootstrap.Instance.Services.TryGet<BlindDelivery.Core.Upgrades.IUpgradeService>(out var upgrades))
            {
                int boxLevel = upgrades.Level(BlindDelivery.Core.Upgrades.UpgradeKind.BoxCapacity);
                if (boxLevel == 1) crateCount = Mathf.Min(data.MaxCrateCount, 4);
                else if (boxLevel >= 2) crateCount = data.MaxCrateCount;
            }
            Crates = new CrateStack(crateCount);

            if (interactions == null) interactions = FindAnyObjectByType<ItemInteractionSystem>();
            if (itemController == null) itemController = FindAnyObjectByType<ItemPlacementController>();
            interactions?.Initialize(data, this);

            world = PhysicsWorld.defaultWorld;
            previousSimulationType = world.simulationType;
            world.simulationType = PhysicsWorld.SimulationType.Script;
            world.gravity = new Vector2(0f, -data.Gravity);
            worldReady = true;
            return true;
        }

        void Update()
        {
            if (!worldReady && !TryInitialize()) return;
            accumulator += Mathf.Min(Time.deltaTime, maxCatchUpSeconds);
            while (accumulator >= fixedStep)
            {
                Step(fixedStep);
                accumulator -= fixedStep;
            }
        }

        void Step(float deltaTime)
        {
            // Tech Plan §4.1: cooldowns tick first, on the same deterministic fixed step.
            itemController?.TickCooldowns(deltaTime);

            deliverymanMotor.ReadPosition(out float x, out float y);

            if (!runEnded && prepRemaining <= 0f &&
                CrateFallRules.IsOutOfWorld(y, geometry.LevelBounds.min.y, fallOutMargin))
            {
                // GDD §3: any fall past the safe height loses a crate — including a fall into the
                // void, where the state machine never raises FallLanded. A forced over-limit value
                // reuses the same crate-loss rule and event as an ordinary landing.
                OnFallLanded(safeFallHeightCells + 1f);
                EndRun(delivered: false);
                return;
            }

            if (!runEnded && prepRemaining > 0f)
            {
                // GDD §2: the player may place items during prep, but the courier waits at A.
                // Holding zero velocity every step also stops the Core body drifting under gravity.
                prepRemaining = Mathf.Max(0f, prepRemaining - deltaTime);
                if (prepRemaining <= 0f && !timerStarted)
                {
                    timer.Start();
                    timerStarted = true;
                }
                deliverymanMotor.SetVelocity(0f, 0f);
                world.Simulate(deltaTime);
                itemController?.StepItems(deltaTime);
                deliverymanMotor.StabilizeRotation();
                SyncTransform();
                return;
            }

            if (runEnded)
            {
                // The result screen is up: let placed items settle but freeze the courier.
                world.Simulate(deltaTime);
                itemController?.StepItems(deltaTime);
                return;
            }

            if (!timerStarted)
            {
                // Legacy scenes with no prep countdown start counting on the first active step.
                timer.Start();
                timerStarted = true;
            }
            timer.Step(deltaTime);

            interactions?.BeforeTick(deltaTime, x, y);
            Stepped?.Invoke(deltaTime);
            MovementSensorResult reading = sensor.Scan(x, y, machine.Facing);
            machine.Tick(deltaTime, reading, deliverymanMotor);

            world.Simulate(deltaTime);
            itemController?.StepItems(deltaTime);
            deliverymanMotor.StabilizeRotation();
            SyncTransform();

            deliverymanMotor.ReadPosition(out x, out y);
            Vector2 end = level.EndPoint != null
                ? (Vector2)level.EndPoint.position
                : new Vector2(x, y);
            if (Vector2.Distance(new Vector2(x, y), end) <= deliveryRadius)
            {
                machine.Deliver(deliverymanMotor);
                EndRun(delivered: true);
            }
        }

        void SyncTransform()
        {
            deliverymanMotor.ReadPosition(out float x, out float y);
            deliverymanMotor.transform.position = new Vector3(x, y, deliverymanMotor.transform.position.z);
        }

        void EndRun(bool delivered)
        {
            if (runEnded) return;
            runEnded = true;
            timer.Stop();
            if (delivered) RunDelivered?.Invoke();
            else RunFailed?.Invoke();
        }

        void OnFallLanded(float fallCells)
        {
            GuidanceItem cushion = null;
            bool cushioned = interactions != null && interactions.IsCushionedLanding(out cushion);
            bool lost = Crates != null && Crates.ApplyFall(fallCells, safeFallHeightCells, cushioned);
            if (lost)
            {
                deliverymanMotor.ReadPosition(out float x, out float y);
                CrateLost?.Invoke(new Vector2(x, y));
            }

            if (cushioned && cushion != null) cushion.GetComponent<CardboxDestroyEffect>()?.Play();
            if (Crates != null && Crates.Count == 0) EndRun(delivered: false);
        }

        void OnDisable()
        {
            if (machine != null) machine.FallLanded -= OnFallLanded;
            if (worldReady)
            {
                world.simulationType = previousSimulationType;
                worldReady = false;
            }
        }
    }
}
