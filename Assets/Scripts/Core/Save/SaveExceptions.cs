using System;

namespace Template.Core.Save
{
    /// <summary>
    /// The stored document is at a schema version this game build cannot read: older than the
    /// first migration, beyond the last one, or no chain at all covers the gap. Surfaced rather
    /// than defaulted so a silent "your progress reset" never ships.
    /// </summary>
    public class SaveVersionException : Exception
    {
        public SaveVersionException(string message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// Main and backup both failed to parse. The data cannot be interpreted; throwing here is the
    /// last resort after the backup route also failed, and callers decide between a reset flow
    /// and reporting — not the save service.
    /// </summary>
    public class SaveCorruptException : Exception
    {
        public SaveCorruptException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}