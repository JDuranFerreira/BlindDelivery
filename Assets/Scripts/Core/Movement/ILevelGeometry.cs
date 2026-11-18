namespace BlindDelivery.Core.Movement
{
    /// <summary>Solid-space query over level geometry. Used by the gameplay-side movement sensor.</summary>
    public interface ILevelGeometry
    {
        bool Contains(float x, float y);
    }
}
