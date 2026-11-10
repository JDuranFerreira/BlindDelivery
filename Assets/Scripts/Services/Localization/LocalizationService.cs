using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Template.Core.Localization;
using Template.Core.Services;

namespace Template.Services.Localization
{
    /// <summary>
    /// CSV/JSON fallback implementation of ILocalizationService.
    /// No external package dependency — reads localization tables from Resources/Localization/{locale}.json
    /// or Resources/Localization/{locale}.csv at runtime.
    ///
    /// File format (JSON):
    /// {
    ///   "key": "value",
    ///   "key_plural": { "zero": "none", "one": "one", "two": "two", "other": "{0} items" }
    /// }
    ///
    /// File format (CSV): key,value (plural forms as key_plural_zero, key_plural_one, etc.)
    /// </summary>
    [AddComponentMenu("Template/Localization/Localization Service")]
    public sealed class LocalizationService : MonoBehaviour, ILocalizationService
    {
        public const string ServiceName = "Localization";

        [Header("Settings")]
        [SerializeField] string fallbackLocale = "en";
        [SerializeField] bool loadFromResources = true;
        [SerializeField] string resourcesPath = "Localization"; // Resources/Localization/

        public string Name => ServiceName;

        public event Action<string> OnLocaleChanged;

        public string CurrentLocale { get; private set; }

        public IReadOnlyList<string> AvailableLocales { get; private set; } = Array.Empty<string>();

        readonly Dictionary<string, Dictionary<string, string>> tables = new Dictionary<string, Dictionary<string, string>>();
        readonly Dictionary<string, Dictionary<string, Dictionary<int, string>>> pluralTables = new Dictionary<string, Dictionary<string, Dictionary<int, string>>>();

        public async Task InitializeAsync(CancellationToken token = default)
        {
            if (loadFromResources)
            {
                LoadFromResources();
            }

            // On first launch, try to detect device language
            if (PlayerPrefs.GetInt("LocaleInitialized", 0) == 0)
            {
                string deviceLang = Application.systemLanguage.ToString().ToLowerInvariant();
                string mapped = MapSystemLanguage(deviceLang);
                if (AvailableLocales.Contains(mapped))
                {
                    await SetLocaleAsync(mapped, token);
                }
                else if (AvailableLocales.Contains(fallbackLocale))
                {
                    await SetLocaleAsync(fallbackLocale, token);
                }
                PlayerPrefs.SetInt("LocaleInitialized", 1);
                PlayerPrefs.Save();
            }
            // On subsequent launches, use saved locale
            else
            {
                string saved = PlayerPrefs.GetString("SelectedLocale", fallbackLocale);
                if (AvailableLocales.Contains(saved))
                {
                    await SetLocaleAsync(saved, token);
                }
                else if (AvailableLocales.Contains(fallbackLocale))
                {
                    await SetLocaleAsync(fallbackLocale, token);
                }
            }
        }

        void LoadFromResources()
        {
            var assets = Resources.LoadAll<TextAsset>(resourcesPath);
            var localeCodes = new HashSet<string>();

            foreach (var asset in assets)
            {
                string localeCode = Path.GetFileNameWithoutExtension(asset.name);
                localeCodes.Add(localeCode);

                var table = new Dictionary<string, string>();
                var pluralTable = new Dictionary<string, Dictionary<int, string>>();

                if (asset.name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                {
                    ParseJson(asset.text, table, pluralTable);
                }
                else if (asset.name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    ParseCsv(asset.text, table, pluralTable);
                }

                tables[localeCode] = table;
                pluralTables[localeCode] = pluralTable;
            }

            AvailableLocales = localeCodes.ToList();
            CurrentLocale = fallbackLocale;
        }

        void ParseJson(string json, Dictionary<string, string> table, Dictionary<string, Dictionary<int, string>> pluralTable)
        {
            // Simple JSON parsing without external deps
            // Expected format: {"key": "value", "key_plural": {"zero": "...", "one": "...", "other": "..."}}
            var matches = Regex.Matches(json, @"""([^""]+)""\s*:\s*(?:""([^""]*)""|\{([^}]+)\})");

            foreach (Match m in matches)
            {
                string key = m.Groups[1].Value;
                string simpleValue = m.Groups[2].Value;
                string objectValue = m.Groups[3].Value;

                if (!string.IsNullOrEmpty(simpleValue))
                {
                    table[key] = UnescapeJson(simpleValue);
                }
                else if (!string.IsNullOrEmpty(objectValue))
                {
                    // Parse plural forms: "zero": "...", "one": "...", "other": "..."
                    var pluralDict = new Dictionary<int, string>();
                    var pluralMatches = Regex.Matches(objectValue, @"""([^""]+)""\s*:\s*""([^""]*)""");
                    foreach (Match pm in pluralMatches)
                    {
                        string form = pm.Groups[1].Value;
                        string value = UnescapeJson(pm.Groups[2].Value);
                        int count = PluralFormToCount(form);
                        pluralDict[count] = value;
                    }
                    if (pluralDict.Count > 0)
                    {
                        pluralTable[key] = pluralDict;
                    }
                }
            }
        }

        void ParseCsv(string csv, Dictionary<string, string> table, Dictionary<string, Dictionary<int, string>> pluralTable)
        {
            var lines = csv.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            bool firstLine = true;

            foreach (var line in lines)
            {
                if (firstLine) { firstLine = false; continue; } // skip header

                var parts = ParseCsvLine(line);
                if (parts.Length < 2) continue;

                string key = parts[0].Trim();
                string value = parts[1].Trim();

                if (key.EndsWith("_plural_zero", StringComparison.OrdinalIgnoreCase))
                {
                    string baseKey = key.Substring(0, key.Length - "_plural_zero".Length);
                    AddPlural(pluralTable, baseKey, 0, value);
                }
                else if (key.EndsWith("_plural_one", StringComparison.OrdinalIgnoreCase))
                {
                    string baseKey = key.Substring(0, key.Length - "_plural_one".Length);
                    AddPlural(pluralTable, baseKey, 1, value);
                }
                else if (key.EndsWith("_plural_two", StringComparison.OrdinalIgnoreCase))
                {
                    string baseKey = key.Substring(0, key.Length - "_plural_two".Length);
                    AddPlural(pluralTable, baseKey, 2, value);
                }
                else if (key.EndsWith("_plural_other", StringComparison.OrdinalIgnoreCase))
                {
                    string baseKey = key.Substring(0, key.Length - "_plural_other".Length);
                    AddPlural(pluralTable, baseKey, 99, value); // 99 = other
                }
                else
                {
                    table[key] = value;
                }
            }
        }

        void AddPlural(Dictionary<string, Dictionary<int, string>> pluralTable, string key, int count, string value)
        {
            if (!pluralTable.ContainsKey(key))
                pluralTable[key] = new Dictionary<int, string>();
            pluralTable[key][count] = value;
        }

        static string[] ParseCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            string current = "";

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current += '"';
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current);
                    current = "";
                }
                else
                {
                    current += c;
                }
            }
            result.Add(current);
            return result.ToArray();
        }

        static string UnescapeJson(string s)
        {
            return s.Replace("\\\"", "\"").Replace("\\n", "\n").Replace("\\t", "\t").Replace("\\\\", "\\");
        }

        public string Get(string key)
        {
            if (tables.TryGetValue(CurrentLocale, out var table) && table.TryGetValue(key, out var value))
                return value;

            // Fallback to fallbackLocale
            if (CurrentLocale != fallbackLocale && tables.TryGetValue(fallbackLocale, out var fallbackTable) && fallbackTable.TryGetValue(key, out var fallbackValue))
                return fallbackValue;

            return key; // fail-visible
        }

        public string Get(string key, params object[] args)
        {
            string format = Get(key);
            if (format == key) return key;
            try { return string.Format(format, args); }
            catch { return format; }
        }

        public string GetPlural(string key, int count, params object[] args)
        {
            if (pluralTables.TryGetValue(CurrentLocale, out var pluralTable) && pluralTable.TryGetValue(key, out var forms))
            {
                string value = SelectPluralForm(forms, count);
                if (value != null)
                {
                    try
                    {
                        var argsArray = new object[args.Length + 1];
                        argsArray[0] = count;
                        Array.Copy(args, 0, argsArray, 1, args.Length);
                        return string.Format(value, argsArray);
                    }
                    catch { return value; }
                }
            }

            // Fallback to fallbackLocale
            if (CurrentLocale != fallbackLocale && pluralTables.TryGetValue(fallbackLocale, out var fallbackPlural) && fallbackPlural.TryGetValue(key, out var fallbackForms))
            {
                string value = SelectPluralForm(fallbackForms, count);
                if (value != null)
                {
                    try
                    {
                        var argsArray = new object[args.Length + 1];
                        argsArray[0] = count;
                        Array.Copy(args, 0, argsArray, 1, args.Length);
                        return string.Format(value, argsArray);
                    }
                    catch { return value; }
                }
            }

            // Fallback to base key
            return Get(key, args);
        }

        static string SelectPluralForm(Dictionary<int, string> forms, int count)
        {
            // English plural rules: 0=zero, 1=one, 2=two, other
            if (forms.TryGetValue(count, out var exact)) return exact;
            if (count == 0 && forms.TryGetValue(0, out var zero)) return zero;
            if (count == 1 && forms.TryGetValue(1, out var one)) return one;
            if (count == 2 && forms.TryGetValue(2, out var two)) return two;
            if (forms.TryGetValue(99, out var other)) return other;
            // Return first available
            foreach (var kv in forms) return kv.Value;
            return null;
        }

        static int PluralFormToCount(string form)
        {
            return form.ToLowerInvariant() switch
            {
                "zero" => 0,
                "one" => 1,
                "two" => 2,
                "other" => 99,
                _ => 99
            };
        }

        public async Task<bool> SetLocaleAsync(string localeCode, CancellationToken token = default)
        {
            if (!AvailableLocales.Contains(localeCode))
                return false;

            CurrentLocale = localeCode;
            PlayerPrefs.SetString("SelectedLocale", localeCode);
            PlayerPrefs.Save();

            OnLocaleChanged?.Invoke(localeCode);
            await Task.Yield();
            return true;
        }

        static string MapSystemLanguage(string systemLang)
        {
            return systemLang switch
            {
                "english" => "en",
                "german" => "de",
                "french" => "fr",
                "spanish" => "es",
                "italian" => "it",
                "japanese" => "ja",
                "korean" => "ko",
                "chinese" => "zh",
                "chinesesimplified" => "zh",
                "chinesetraditional" => "zh-TW",
                "russian" => "ru",
                "portuguese" => "pt",
                "polish" => "pl",
                "turkish" => "tr",
                "arabic" => "ar",
                _ => "en"
            };
        }

        /// <summary>
        /// Add or update a locale table at runtime (for testing or dynamic loading).
        /// </summary>
        public void AddLocaleTable(string localeCode, Dictionary<string, string> entries, Dictionary<string, Dictionary<int, string>> pluralEntries = null)
        {
            tables[localeCode] = entries;
            pluralTables[localeCode] = pluralEntries ?? new Dictionary<string, Dictionary<int, string>>();

            if (!AvailableLocales.Contains(localeCode))
            {
                var list = new List<string>(AvailableLocales);
                list.Add(localeCode);
                AvailableLocales = list;
            }
        }
    }
}