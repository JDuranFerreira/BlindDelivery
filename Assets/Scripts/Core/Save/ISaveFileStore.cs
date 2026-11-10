using System.Threading.Tasks;

namespace Template.Core.Save
{
    /// <summary>
    /// Raw document keys to strings. Deliberately dumber than <see cref="ISaveService"/>:
    /// no versioning, no parsing, no events — the file store knows how to put a string on disk
    /// safely, and only that. The default implementation keeps temp-swap atomicity and one
    /// generation of backup per key; a fake in-memory store serves the EditMode tests.
    ///
    /// No cancellation token: a write interrupted by shutdown must still finish or must not
    /// start, which is the atomicity contract itself.
    /// </summary>
    public interface ISaveFileStore
    {
        /// <summary>True when the main document exists on disk (backup files do not count).</summary>
        bool Exists(string key);

        /// <summary>Main document text, or null when absent.</summary>
        Task<string> ReadMainAsync(string key);

        /// <summary>Previous generation kept by the store, or null when absent.</summary>
        Task<string> ReadBackupAsync(string key);

        /// <summary>Writes the document atomically and keeps the previous main as the backup.</summary>
        Task WriteAsync(string key, string contents);
    }
}