namespace BlindDelivery.Core.Items
{
    /// <summary>
    /// How an authored object is used in a level (GDD §4).
    /// <para><see cref="Guidance"/>: a Guidance Object. It appears in the HUD and the player
    /// places and drags it.</para>
    /// <para><see cref="LevelObject"/>: authored into the level for design only (e.g. a wind
    /// break or spikes). It is never a player tool and cannot be dragged during play.</para>
    /// </summary>
    public enum ItemUsage
    {
        Guidance,
        LevelObject,
    }
}
