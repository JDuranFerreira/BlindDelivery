namespace Template.Core.Events
{
    /// <summary>
    /// Marker for anything allowed to travel on the event bus. It exists so
    /// <c>Publish(someRandomStruct)</c> does not compile, and so the bus can constrain its
    /// generic methods to structs: publishing then allocates nothing, which matters for
    /// per-frame and per-kill style events.
    /// </summary>
    public interface IEvent
    {
    }
}
