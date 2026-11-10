using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Template.Core.Save;

namespace Template.Tests.EditMode.Fakes
{
    /// <summary>
    /// Memory double of the document store. Purpose-built knobs: backing file corruption is
    /// simulated by overwriting the raw strings, exactly what a kill mid-write does to a file.
    /// </summary>
    public sealed class InMemorySaveStore : ISaveFileStore
    {
        readonly Dictionary<string, string> main = new Dictionary<string, string>(StringComparer.Ordinal);
        readonly Dictionary<string, string> backup = new Dictionary<string, string>(StringComparer.Ordinal);

        public bool Exists(string key)
        {
            SaveKey.Validate(key);
            return main.ContainsKey(key);
        }

        public Task<string> ReadMainAsync(string key)
        {
            SaveKey.Validate(key);
            return Task.FromResult(main.TryGetValue(key, out string value) ? value : null);
        }

        public Task<string> ReadBackupAsync(string key)
        {
            SaveKey.Validate(key);
            return Task.FromResult(backup.TryGetValue(key, out string value) ? value : null);
        }

        public Task WriteAsync(string key, string contents)
        {
            SaveKey.Validate(key);
            if (main.TryGetValue(key, out string previous))
            {
                backup[key] = previous;
            }

            main[key] = contents;
            return Task.CompletedTask;
        }

        public void OverwriteMain(string key, string contents)
        {
            main[key] = contents;
        }

        public void OverwriteBackup(string key, string contents)
        {
            backup[key] = contents;
        }

        public void DropMain(string key)
        {
            main.Remove(key);
        }
    }
}