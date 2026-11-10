using System;
using System.Collections.Generic;
using UnityEngine;

namespace Template.Services.Audio
{
    /// <summary>
    /// Key-to-clip table as a ScriptableObject, so a game's sounds are data: gameplay asks for
    /// "hit_small", the library resolves which AudioClip that is this game. A game builds its own
    /// library asset (Create > Template > Audio Library) and hands it to the bootstrap; keys stay
    /// stable in code while clips get swapped without touching it.
    /// </summary>
    [CreateAssetMenu(menuName = "Template/Audio Library", fileName = "AudioLibrary")]
    public sealed class AudioLibrary : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string Key;
            public AudioClip Clip;
        }

        [SerializeField] List<Entry> entries = new List<Entry>();

        public bool TryGetClip(string key, out AudioClip clip)
        {
            clip = null;
            if (string.IsNullOrEmpty(key))
            {
                return false;
            }

            foreach (Entry entry in entries)
            {
                if (string.Equals(entry.Key, key, StringComparison.Ordinal) && entry.Clip != null)
                {
                    clip = entry.Clip;
                    return true;
                }
            }

            return false;
        }

        /// <summary>Registers one entry, replacing an existing key. Editor tooling and tests use it.</summary>
        public void Add(string key, AudioClip clip)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("An audio key is required.", nameof(key));
            }

            for (int i = 0; i < entries.Count; i++)
            {
                if (string.Equals(entries[i].Key, key, StringComparison.Ordinal))
                {
                    entries[i] = new Entry
                    {
                        Key = key,
                        Clip = clip,
                    };
                    return;
                }
            }

            entries.Add(new Entry
            {
                Key = key,
                Clip = clip,
            });
        }
    }
}