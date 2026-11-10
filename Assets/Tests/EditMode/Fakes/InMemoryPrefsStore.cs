using System.Collections.Generic;
using Template.Core.Settings;

namespace Template.Tests.EditMode.Fakes
{
    /// <summary>Memory double of PlayerPrefs; typed boxes keep floats, ints and strings apart.</summary>
    public sealed class InMemoryPrefsStore : IPrefsStore
    {
        readonly Dictionary<string, object> values = new Dictionary<string, object>();

        public int SaveCount { get; private set; }

        public bool TryGetFloat(string key, out float value) => TryGet(key, out value);

        public void SetFloat(string key, float value) => values[key] = value;

        public bool TryGetInt(string key, out int value) => TryGet(key, out value);

        public void SetInt(string key, int value) => values[key] = value;

        public bool TryGetString(string key, out string value) => TryGet(key, out value);

        public void SetString(string key, string value) => values[key] = value;

        public void Save() => SaveCount++;

        bool TryGet<TValue>(string key, out TValue value)
        {
            if (values.TryGetValue(key, out object boxed) && boxed is TValue typed)
            {
                value = typed;
                return true;
            }

            value = default;
            return false;
        }
    }
}