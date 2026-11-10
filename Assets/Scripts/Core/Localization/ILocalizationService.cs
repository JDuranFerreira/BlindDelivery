using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Template.Core.Services;

namespace Template.Core.Localization
{
    /// <summary>
    /// Localization seam — all text goes through keys, never hard-coded strings.
    /// Implementation wraps Unity Localization package (or a CSV/JSON fallback).
    /// </summary>
    public interface ILocalizationService : IAppService
    {
        /// <summary>Raised when the active locale changes.</summary>
        event Action<string> OnLocaleChanged;

        /// <summary>Current locale code (e.g., "en", "de", "ja", "ar").</summary>
        string CurrentLocale { get; }

        /// <summary>All available locale codes.</summary>
        IReadOnlyList<string> AvailableLocales { get; }

        /// <summary>
        /// Get a localized string by key. Returns the key itself if not found (fail-visible).
        /// </summary>
        string Get(string key);

        /// <summary>
        /// Get a localized string with formatted arguments (e.g., "{0} coins" with args).
        /// </summary>
        string Get(string key, params object[] args);

        /// <summary>
        /// Get a pluralized string. Unity Localization handles plural rules per locale.
        /// </summary>
        string GetPlural(string key, int count, params object[] args);

        /// <summary>
        /// Switch locale at runtime. Returns true if the locale was available and switched.
        /// </summary>
        Task<bool> SetLocaleAsync(string localeCode, CancellationToken token = default);

        /// <summary>
        /// Initialize with device language detection (first launch) or last saved locale.
        /// </summary>
        Task InitializeAsync(CancellationToken token = default);
    }

    /// <summary>
    /// Simple table interface for CSV/JSON fallback if Unity Localization package isn't used.
    /// </summary>
    public interface ILocalizationTable
    {
        /// <summary>Locale code this table belongs to (e.g., "en", "de").</summary>
        string LocaleCode { get; }

        /// <summary>Try get a value by key.</summary>
        bool TryGet(string key, out string value);

        /// <summary>Try get a plural form by key and count.</summary>
        bool TryGetPlural(string key, int count, out string value);
    }
}