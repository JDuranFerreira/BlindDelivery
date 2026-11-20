using BlindDelivery.Core.Movement;
using UnityEngine;

namespace BlindDelivery.Gameplay.Movement
{
    /// <summary>
    /// Renders the deliveryman's facing direction. The state machine owns the logical facing;
    /// this view only mirrors the sprite, so visuals never drive gameplay rules.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class DeliverymanView : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;

        IMovementStateProvider stateProvider;
        Animator animator;

        void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();

            SimulationRunner runner = FindAnyObjectByType<SimulationRunner>();
            if (runner != null) stateProvider = runner.Machine;
        }

        void LateUpdate()
        {
            // The runner may finish initializing after the deliveryman is spawned.
            if (stateProvider == null)
            {
                SimulationRunner runner = FindAnyObjectByType<SimulationRunner>();
                if (runner != null) stateProvider = runner.Machine;
            }

            if (stateProvider != null && spriteRenderer != null)
            {
                spriteRenderer.flipX = stateProvider.Facing < 0;
            }

            // Idle is the default; only a walking courier plays the walk cycle.
            if (animator != null && stateProvider != null)
            {
                animator.SetBool("Walking", stateProvider.State == MovementState.Walking);
            }
        }
    }
}
