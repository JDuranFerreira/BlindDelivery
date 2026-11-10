using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Template.Core.Localization;
using Template.Core.Services;

namespace Template.Tests.EditMode
{
    /// <summary>
    /// Tests the localization contracts: key lookup, plural forms, locale switching, fallback behavior.
    /// Uses pure-C# fakes so tests run in EditMode without Unity engine or Localization package.
    /// </summary>
    public class LocalizationTests
    {
        [Test]
        public void LocalizationTable_TryGet_ReturnsValueOrFalse()
        {
            var table = new FakeLocalizationTable("en");
            table.Add("hello", "Hello World");
            table.Add("farewell", "Goodbye");

            Assert.That(table.TryGet("hello", out var value), Is.True);
            Assert.That(value, Is.EqualTo("Hello World"));

            Assert.That(table.TryGet("missing", out _), Is.False);
        }

        [Test]
        public void LocalizationTable_TryGetPlural_ReturnsCorrectForm()
        {
            var table = new FakeLocalizationTable("en");
            table.AddPlural("item", 0, "no items");
            table.AddPlural("item", 1, "one item");
            table.AddPlural("item", 2, "two items");
            table.AddPlural("item", 5, "many items");

            Assert.That(table.TryGetPlural("item", 0, out var v0), Is.True);
            Assert.That(v0, Is.EqualTo("no items"));

            Assert.That(table.TryGetPlural("item", 1, out var v1), Is.True);
            Assert.That(v1, Is.EqualTo("one item"));

            Assert.That(table.TryGetPlural("item", 2, out var v2), Is.True);
            Assert.That(v2, Is.EqualTo("two items"));

            Assert.That(table.TryGetPlural("item", 5, out var v5), Is.True);
            Assert.That(v5, Is.EqualTo("many items"));

            // Missing key
            Assert.That(table.TryGetPlural("missing", 1, out _), Is.False);
        }

        [Test]
        public void FakeLocalizationService_Get_ReturnsKeyIfMissing()
        {
            var service = new FakeLocalizationService();
            service.AddTable("en", new Dictionary<string, string> { { "greeting", "Hello" } });

            string result = service.Get("greeting");
            Assert.That(result, Is.EqualTo("Hello"));

            string missing = service.Get("nonexistent");
            Assert.That(missing, Is.EqualTo("nonexistent")); // fail-visible: returns key
        }

        [Test]
        public void FakeLocalizationService_GetWithArgs_FormatsString()
        {
            var service = new FakeLocalizationService();
            service.AddTable("en", new Dictionary<string, string> { { "score", "Score: {0}" } });

            string result = service.Get("score", 100);
            Assert.That(result, Is.EqualTo("Score: 100"));
        }

        [Test]
        public void FakeLocalizationService_GetPlural_SelectsCorrectForm()
        {
            var service = new FakeLocalizationService();
            var table = new FakeLocalizationTable("en");
            table.AddPlural("coin", 0, "no coins");
            table.AddPlural("coin", 1, "one coin");
            table.AddPlural("coin", 2, "two coins");
            table.AddPlural("coin", 5, "{0} coins");
            service.AddTable("en", table);

            Assert.That(service.GetPlural("coin", 0), Is.EqualTo("no coins"));
            Assert.That(service.GetPlural("coin", 1), Is.EqualTo("one coin"));
            Assert.That(service.GetPlural("coin", 2), Is.EqualTo("two coins"));
            Assert.That(service.GetPlural("coin", 5), Is.EqualTo("5 coins"));
        }

        [Test]
        public void FakeLocalizationService_SetLocaleAsync_SwitchesAndPersists()
        {
            var service = new FakeLocalizationService();
            service.AddTable("en", new Dictionary<string, string> { { "greeting", "Hello" } });
            service.AddTable("de", new Dictionary<string, string> { { "greeting", "Hallo" } });

            Assert.That(service.CurrentLocale, Is.EqualTo("en"));

            var switched = service.SetLocaleAsync("de").Result;
            Assert.That(switched, Is.True);
            Assert.That(service.CurrentLocale, Is.EqualTo("de"));
            Assert.That(service.Get("greeting"), Is.EqualTo("Hallo"));

            // Switch back
            switched = service.SetLocaleAsync("en").Result;
            Assert.That(switched, Is.True);
            Assert.That(service.CurrentLocale, Is.EqualTo("en"));
            Assert.That(service.Get("greeting"), Is.EqualTo("Hello"));
        }

        [Test]
        public void FakeLocalizationService_SetLocaleAsync_InvalidLocale_ReturnsFalse()
        {
            var service = new FakeLocalizationService();
            service.AddTable("en", new Dictionary<string, string> { { "greeting", "Hello" } });

            var switched = service.SetLocaleAsync("fr").Result;
            Assert.That(switched, Is.False);
            Assert.That(service.CurrentLocale, Is.EqualTo("en"));
        }

        [Test]
        public void FakeLocalizationService_AvailableLocales_ReturnsRegisteredLocales()
        {
            var service = new FakeLocalizationService();
            service.AddTable("en", new Dictionary<string, string>());
            service.AddTable("de", new Dictionary<string, string>());
            service.AddTable("ja", new Dictionary<string, string>());

            var locales = service.AvailableLocales;
            Assert.That(locales, Has.Count.EqualTo(3));
            Assert.That(locales, Does.Contain("en"));
            Assert.That(locales, Does.Contain("de"));
            Assert.That(locales, Does.Contain("ja"));
        }

        [Test]
        public async Task FakeLocalizationService_InitializeAsync_Completes()
        {
            var service = new FakeLocalizationService();
            service.AddTable("en", new Dictionary<string, string> { { "test", "Test" } });

            await service.InitializeAsync();
            Assert.Pass();
        }

        [Test]
        public void FakeLocalizationService_OnLocaleChanged_FiredOnSwitch()
        {
            var service = new FakeLocalizationService();
            service.AddTable("en", new Dictionary<string, string>());
            service.AddTable("de", new Dictionary<string, string>());

            var changedLocales = new List<string>();
            service.OnLocaleChanged += locale => changedLocales.Add(locale);

            service.SetLocaleAsync("de").Wait();

            Assert.That(changedLocales, Has.Count.EqualTo(1));
            Assert.That(changedLocales[0], Is.EqualTo("de"));
        }
    }

    /// <summary>
    /// Fake ILocalizationTable for testing.
    /// </summary>
    public sealed class FakeLocalizationTable : ILocalizationTable
    {
        readonly Dictionary<string, string> entries = new Dictionary<string, string>();
        readonly Dictionary<string, Dictionary<int, string>> pluralEntries = new Dictionary<string, Dictionary<int, string>>();

        public FakeLocalizationTable(string localeCode)
        {
            LocaleCode = localeCode;
        }

        public string LocaleCode { get; }

        public void Add(string key, string value) => entries[key] = value;

        public void AddPlural(string key, int count, string value)
        {
            if (!pluralEntries.ContainsKey(key))
                pluralEntries[key] = new Dictionary<int, string>();
            pluralEntries[key][count] = value;
        }

        public bool TryGet(string key, out string value) => entries.TryGetValue(key, out value);

        public bool TryGetPlural(string key, int count, out string value)
        {
            Dictionary<int, string> dict;
            if (pluralEntries.TryGetValue(key, out dict) && dict.TryGetValue(count, out value))
                return true;

            value = null;
            return false;
        }
    }

    /// <summary>
    /// Fake ILocalizationService for EditMode testing.
    /// </summary>
    public sealed class FakeLocalizationService : ILocalizationService
    {
        readonly Dictionary<string, object> tables = new Dictionary<string, object>();
        string currentLocale = "en";

        public string Name => "FakeLocalization";

        public event Action<string> OnLocaleChanged;

        public string CurrentLocale => currentLocale;

        public IReadOnlyList<string> AvailableLocales => new List<string>(tables.Keys);

        public void AddTable(string localeCode, IDictionary<string, string> entries)
        {
            var table = new FakeLocalizationTable(localeCode);
            foreach (var kv in entries)
                table.Add(kv.Key, kv.Value);
            tables[localeCode] = table;
        }

        public void AddTable(string localeCode, FakeLocalizationTable table)
        {
            tables[localeCode] = table;
        }

        public string Get(string key)
        {
            if (tables.TryGetValue(currentLocale, out var tableObj) && tableObj is FakeLocalizationTable table)
            {
                if (table.TryGet(key, out var value))
                    return value;
            }
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
            if (tables.TryGetValue(currentLocale, out var tableObj) && tableObj is FakeLocalizationTable table)
            {
                if (table.TryGetPlural(key, count, out var value))
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
            return key;
        }

        public Task<bool> SetLocaleAsync(string localeCode, System.Threading.CancellationToken token = default)
        {
            if (!tables.ContainsKey(localeCode))
                return Task.FromResult(false);

            currentLocale = localeCode;
            OnLocaleChanged?.Invoke(localeCode);
            return Task.FromResult(true);
        }

        public Task InitializeAsync(System.Threading.CancellationToken token = default)
        {
            return Task.CompletedTask;
        }
    }
}