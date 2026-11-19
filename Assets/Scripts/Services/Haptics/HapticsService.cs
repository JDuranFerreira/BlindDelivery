using System.Threading;
using System.Threading.Tasks;
using BlindDelivery.Core.Haptics;
using BlindDelivery.Core.Services;
using BlindDelivery.Core.Settings;
using UnityEngine;

namespace BlindDelivery.Services.Haptics
{
    /// <summary>
    /// Mobile haptics behind the Core seam. Desktop and the Editor get a supported=false no-op;
    /// a player who switches vibration off is respected on every cue. Patterns differ only in
    /// intensity here because Handheld.Vibrate is one fixed pulse on most Android devices — iOS
    /// Taptic patterns can replace the body of Play without touching any caller.
    /// </summary>
    public sealed class HapticsService : IAppService, IHapticsService
    {
        const string ServiceName = "Haptics";

        readonly ISettingsService settings;

        public HapticsService(ISettingsService settings)
        {
            this.settings = settings ?? throw new System.ArgumentNullException(nameof(settings));
        }

        public string Name => ServiceName;

        public bool IsSupported => Application.isMobilePlatform;

        public Task InitializeAsync(CancellationToken token = default) => Task.CompletedTask;

        public void Play(HapticPattern pattern, float intensity = 1f)
        {
            if (!settings.VibrationEnabled || !IsSupported) return;
            if (intensity <= 0f) return;

            Handheld.Vibrate();
        }
    }
}
