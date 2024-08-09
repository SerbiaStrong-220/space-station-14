namespace Content.Shared.SS220.Spray.Events;

/// <summary>
/// Raised on an AmmoProvider to request deets.
/// </summary>
[ByRefEvent]
public struct GetSolutionCountEvent
{
    public int Count;
    public int Capacity;
}
