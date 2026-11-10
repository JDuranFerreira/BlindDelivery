using System;
using System.IO;
using System.Threading.Tasks;
using Template.Core.Save;
using UnityEngine;

namespace Template.Services.Save
{
    /// <summary>
    /// The disk half of saving: one UTF-8 text document per key under
    /// <c>Application.persistentDataPath/saves</c>, written atomically and one generation of
    /// backup. A kill at any point of a write leaves either the previous or the new document
    /// readable - the acceptance check "kill mid-write and the save still loads". The service above
    /// this file decides what a document means; this type only knows how to swap files safely.
    ///
    /// Reads complete synchronously (they are small documents): the async path exists for the
    /// seam, not for off-threading.
    /// </summary>
    public sealed class FileSaveStore : ISaveFileStore
    {
        const string DefaultFolderName = "saves";
        const string MainSuffix = ".json";
        const string TempSuffix = ".tmp";
        const string BackupSuffix = ".bak";

        readonly string root;

        /// <summary>directory is a seam for tests; production leaves it unset for persistentDataPath.</summary>
        public FileSaveStore(string directory = null)
        {
            root = string.IsNullOrEmpty(directory)
                ? Path.Combine(Application.persistentDataPath, DefaultFolderName)
                : directory;
        }

        public bool Exists(string key)
        {
            SaveKey.Validate(key);
            return File.Exists(MainPath(key));
        }

        public Task<string> ReadMainAsync(string key)
        {
            SaveKey.Validate(key);
            return ReadAsync(MainPath(key));
        }

        public Task<string> ReadBackupAsync(string key)
        {
            SaveKey.Validate(key);
            return ReadAsync(BackupPath(key));
        }

        public Task WriteAsync(string key, string contents)
        {
            SaveKey.Validate(key);
            string mainPath = MainPath(key);
            Directory.CreateDirectory(root);

            // 1. The current main file, if any, becomes the backup before anything else moves.
            if (File.Exists(mainPath))
            {
                File.Copy(mainPath, BackupPath(key), overwrite: true);
            }

            // 2. New contents land on a temp file...
            string tempPath = TempPath(key);
            File.WriteAllText(tempPath, contents);

            // 3. ...and the swap is one move. A crash before it leaves the old main and the fresh
            //    backup; a crash during a Future-formatted write leaves the temp file, which is
            //    garbage and ignored by every reader.
            if (File.Exists(mainPath))
            {
                File.Delete(mainPath);
            }

            File.Move(tempPath, mainPath);
            return Task.CompletedTask;
        }

        Task<string> ReadAsync(string path)
        {
            if (!File.Exists(path))
            {
                return Task.FromResult<string>(null);
            }

            return Task.FromResult(File.ReadAllText(path));
        }

        string MainPath(string key) => Path.Combine(root, key + MainSuffix);

        string TempPath(string key) => Path.Combine(root, key + MainSuffix + TempSuffix);

        string BackupPath(string key) => Path.Combine(root, key + MainSuffix + BackupSuffix);
    }
}