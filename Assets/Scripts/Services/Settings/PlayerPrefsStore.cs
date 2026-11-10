using Template.Core.Settings;
using UnityEngine;

namespace Template.Services.Settings
{
    /// <summary>
    /// The real settings persistence: Unity PlayerPrefs. Mechanical on purpose - every decision
    /// lives in <see cref="SettingsService"/>, here is only the engine call.
    /// </summary>
    public sealed class PlayerPrefsStore : IPrefsStore
    {
        public bool TryGetFloat(string key, out float value)
        {
            if (PlayerPrefs.HasKey(key))
            {
                value = PlayerPrefs.GetFloat(key);
                return true;
            }

            value = 0f;
            return false;
        }

        public void SetFloat(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
        }

        public bool TryGetInt(string key, out int value)
        {
            if (PlayerPrefs.HasKey(key))
            {
                value = PlayerPrefs.GetInt(key);
                return true;
            }

            value = 0;
            return false;
        }

        public void SetInt(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
        }

        public bool TryGetString(string key, out string value)
        {
            if (PlayerPrefs.HasKey(key))
            {
                value = PlayerPrefs.GetString(key);
                return true;
            }

            value = null;
            return false;
        }

        public void SetString(string key, string value)
        {
            PlayerPrefs.SetString(key, value);
        }

        public void Save()
        {
            PlayerPrefs.Save();
        }
    }
}