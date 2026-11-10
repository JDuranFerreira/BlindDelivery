namespace Template.Core.Save
{
    /// <summary>
    /// One step of the save migration chain: rewrites a payload older by exactly one schema
    /// version into the next one. Steps work on the payload text itself, so a migration never
    /// has to know the full document type — adding a field can default it, renaming or splitting
    /// fields is a pure text/JSON rewrite.
    /// </summary>
    public interface ISaveMigration
    {
        /// <summary>Schema version the step accepts. The first registered step defines the oldest supported save.</summary>
        int FromVersion { get; }

        /// <summary>Schema version the step produces. Must be exactly <see cref="FromVersion"/> + 1.</summary>
        int ToVersion { get; }

        /// <summary>Returns the migrated payload JSON. Throws on payload it cannot interpret — that is a bug, not data loss.</summary>
        string Migrate(string payloadJson);
    }
}