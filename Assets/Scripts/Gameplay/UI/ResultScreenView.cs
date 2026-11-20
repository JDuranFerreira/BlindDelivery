using BlindDelivery.Core.Scoring;
using BlindDelivery.Core.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace BlindDelivery.Gameplay.UI
{
    /// <summary>
    /// End-of-run screen: normal stars, gold bonus stars and the efficiency score, plus a single
    /// Replay action so a first-time tester can restart without help (Tech Plan phase 6).
    /// </summary>
    public sealed class ResultScreenView : ToolkitScreenView
    {
        [SerializeField] UnityEngine.Object stateSource;

        Label title;
        Label stars;
        Label bonus;
        Label score;
        Label time;
        Label best;
        IResultState state;

        protected override void Bind(VisualElement root)
        {
            // The result screen starts hidden; UIManager.PushAsync removes this class via ShowAsync.
            Root?.AddToClassList("screen-hidden");

            state = stateSource as IResultState;
            if (state == null)
            {
                foreach (MonoBehaviour candidate in FindObjectsByType<MonoBehaviour>())
                {
                    if (candidate is IResultState found)
                    {
                        state = found;
                        break;
                    }
                }
            }

            title = Find<Label>("ResultTitle");
            stars = Find<Label>("Stars");
            bonus = Find<Label>("Bonus");
            score = Find<Label>("Score");
            time = Find<Label>("Time");
            best = Find<Label>("Best");

            Button replay = Find<Button>("Replay");
            if (replay != null) replay.clicked += () => state?.Replay();
        }

        void Update()
        {
            if (state == null || !state.HasResult) return;

            StarResult result = state.Result;
            SetText(title, result.Failed ? "RUN FAILED" : "DELIVERED");
            SetText(stars, result.Failed ? "No crates reached the door" : result.Rating);
            SetText(bonus, result.PlusPlus ? "++ GOLD BONUS" : result.BonusStars > 0 ? "+ GOLD BONUS" : string.Empty);
            SetText(score, "Score  " + state.Score);
            SetText(time, "Time  " + state.ElapsedSeconds.ToString("0.0") + "s");
            SetText(best, "Best  " + (state.BestSeconds > 0f ? state.BestSeconds.ToString("0.0") + "s" : "-"));
        }
    }
}
