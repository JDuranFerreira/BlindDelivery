using System;
using UnityEngine;

namespace Template.Examples
{
    /// <summary>
    /// Example save document for the SystemsDemo scene (goals note M2). Plain fields only —
    /// JsonUtility is the serializer, so no nested collections. Schema version 2: the demo
    /// registers one migration step (1 -> 2) to show how an old save survives a schema change.
    /// </summary>
    [Serializable]
    public sealed class DemoSaveDocument
    {
        public int coins;
        public int bestScore;

        /// <summary>Added in schema version 2; the migration defaults it on old saves.</summary>
        public int runsPlayed;
    }

    /// <summary>What a version-1 payload looked like, for the migration step below.</summary>
    [Serializable]
    sealed class DemoSaveDocumentV1
    {
        public int coins;
        public int bestScore;
    }

    /// <summary>
    /// Version 1 -> 2: fills the new runsPlayed field with 0 on older saves. Steps are exactly
    /// one version forward and attach to the chain with
    /// <c>ISaveService.RegisterMigration</c> — the first registered step defines the oldest
    /// supported save, and every later step must continue that chain.
    /// </summary>
    public sealed class DemoSaveMigration : Template.Core.Save.ISaveMigration
    {
        public int FromVersion => 1;

        public int ToVersion => 2;

        public string Migrate(string payloadJson)
        {
            DemoSaveDocumentV1 old = JsonUtility.FromJson<DemoSaveDocumentV1>(payloadJson);
            return JsonUtility.ToJson(new DemoSaveDocument
            {
                coins = old.coins,
                bestScore = old.bestScore,
                runsPlayed = 0,
            });
        }
    }
}