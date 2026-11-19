namespace BlindDelivery.Core.Items
{
    /// <summary>
    /// How an authored item is used in a level.
    /// <para><see cref="PlayerTool"/>: appears in the HUD, the player places and drags it.</para>
    /// <para><see cref="LevelFixture"/>: part of the level design only (e.g. a wind zone).
    /// It is not a player tool and cannot be dragged during play.</para>
    /// </summary>
    public enum ItemUsage
    {
        PlayerTool,
        LevelFixture,
    }
}
