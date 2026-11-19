using BlindDelivery.Core.Balance;
using BlindDelivery.Core.Crates;
using BlindDelivery.Core.Movement;
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

        DeliverymanStateMachine machine;
        PhysicsWorld world;
        PhysicsWorld.SimulationType previousSimulationType;
        float fixedStep;
        float accumulator;
        bool worldReady;
        int safeFallHeightCells;

        public DeliverymanStateMachine Machine => machine;

        /// <summary>Crate state for this run; item interactions consume it on unsafe landings.</summary>
        public CrateStack Crates { get; private set; }

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
            fixedStep = data.FixedStepSeconds;
            safeFallHeightCells = data.SafeFallHeightCells;
            geometry.Rebuild();
            machine = new DeliverymanStateMachine(data.DeliverymanSpeed);
            machine.FallLanded += OnFallLanded;
            Crates = new CrateStack(data.BaseCrateCount);

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
            deliverymanMotor.ReadPosition(out float x, out float y);
            interactions?.BeforeTick(deltaTime, x, y);
            MovementSensorResult reading = sensor.Scan(x, y, machine.Facing);
            machine.Tick(deltaTime, reading, deliverymanMotor);

            world.Simulate(deltaTime);
            itemController?.StepItems(deltaTime);
            deliverymanMotor.StabilizeRotation();

            // The Core body moves in the physics world; copy its pose to the Transform so the
            // SpriteRenderer (and any child anchors) follow it.
            deliverymanMotor.ReadPosition(out x, out y);
            deliverymanMotor.transform.position = new Vector3(x, y, deliverymanMotor.transform.position.z);

            Vector2 end = level.EndPoint != null
                ? (Vector2)level.EndPoint.position
                : new Vector2(x, y);
            if (Vector2.Distance(new Vector2(x, y), end) <= deliveryRadius)
            {
                machine.Deliver(deliverymanMotor);
            }
        }

        void OnFallLanded(float fallCells)
        {
            bool cushioned = interactions != null && interactions.IsCushionedLanding();
            Crates?.ApplyFall(fallCells, safeFallHeightCells, cushioned);
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
