using BlindDelivery.Core.Scenes;
using BlindDelivery.Core.Scoring;
using BlindDelivery.Core.UI;
using BlindDelivery.Gameplay.Items;
using BlindDelivery.Gameplay.Movement;
using BlindDelivery.Services.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlindDelivery.Gameplay.Levels
{
    /// <summary>
    /// Closes the vertical-slice loop: watches the simulation, turns the run outcome into stars
    /// and a score, pushes the result screen and reloads the level on Replay. The rules stay in
    /// Core; this component only adapts them to the scene and the UI service.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LevelFlowController : MonoBehaviour, IResultState
    {
        [SerializeField] SimulationRunner runner;
        [SerializeField] ItemPlacementController items;
        [SerializeField] string resultScreenId = "Result";

        public bool HasResult { get; private set; }

        public StarResult Result { get; private set; }

        public int Score { get; private set; }

        void OnEnable()
        {
            if (runner == null) runner = FindAnyObjectByType<SimulationRunner>();
            if (items == null) items = FindAnyObjectByType<ItemPlacementController>();
            if (runner == null) return;

            runner.RunDelivered += OnRunDelivered;
            runner.RunFailed += OnRunFailed;
        }

        void OnDisable()
        {
            if (runner == null) return;
            runner.RunDelivered -= OnRunDelivered;
            runner.RunFailed -= OnRunFailed;
        }

        void OnRunDelivered()
        {
            Finish(StarCalculator.Calculate(runner.Crates != null ? runner.Crates.Count : 0));
        }

        void OnRunFailed()
        {
            Finish(StarCalculator.Calculate(0));
        }

        void Finish(StarResult result)
        {
            if (HasResult) return;

            HasResult = true;
            Result = result;
            Score = items != null && items.Budget != null ? items.Budget.Points : 0;

            if (AppBootstrap.Instance == null ||
                !AppBootstrap.Instance.Services.TryGet<UIManager>(out UIManager ui))
            {
                Debug.LogError("[LevelFlowController] UIManager is not registered; cannot show the result screen.", this);
                return;
            }

            _ = ui.PushAsync(resultScreenId);
        }

        public void Replay()
        {
            string scene = SceneManager.GetActiveScene().name;
            if (AppBootstrap.Instance != null &&
                AppBootstrap.Instance.Services.TryGet<ISceneLoader>(out ISceneLoader loader))
            {
                _ = loader.LoadAsync(scene);
                return;
            }

            SceneManager.LoadScene(scene);
        }
    }
}
