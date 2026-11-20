using BlindDelivery.Core.Scenes;
using BlindDelivery.Core.Save;
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

        const string ResultsKey = "level_results";

        public bool HasResult { get; private set; }

        public StarResult Result { get; private set; }

        public int Score { get; private set; }

        public float ElapsedSeconds { get; private set; }

        public float BestSeconds { get; private set; }

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
            ElapsedSeconds = runner != null ? runner.RunSeconds : 0f;
            _ = PersistResultAsync();

            if (AppBootstrap.Instance == null ||
                !AppBootstrap.Instance.Services.TryGet<UIManager>(out UIManager ui))
            {
                Debug.LogError("[LevelFlowController] UIManager is not registered; cannot show the result screen.", this);
                return;
            }

            _ = ui.PushAsync(resultScreenId);
        }

        async System.Threading.Tasks.Task PersistResultAsync()
        {
            if (AppBootstrap.Instance == null ||
                !AppBootstrap.Instance.Services.TryGet<ISaveService>(out ISaveService save))
            {
                return;
            }

            string levelId = SceneManager.GetActiveScene().name;
            var results = await save.LoadAsync<LevelResultsData>(ResultsKey);
            int stars = Result.Stars + Result.BonusStars;
            results.Record(levelId, ElapsedSeconds, stars);
            await save.SaveAsync(ResultsKey, results);

            float best = results.BestTime(levelId);
            BestSeconds = best < float.MaxValue ? best : 0f;
        }

        public void Replay()
        {
            string scene = SceneManager.GetActiveScene().name;

            // Dismiss the result immediately: the scene load is async, and leaving the overlay up
            // makes the click look ignored even though the reload has already started.
            if (AppBootstrap.Instance != null &&
                AppBootstrap.Instance.Services.TryGet<UIManager>(out UIManager ui))
            {
                _ = ui.BackAsync();
            }

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
