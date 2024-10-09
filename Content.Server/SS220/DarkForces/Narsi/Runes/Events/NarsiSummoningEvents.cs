namespace Content.Server.SS220.DarkForces.Narsi.Runes.Events;

public sealed class NarsiSummoningStartEvent : CancellableEntityEventArgs
{
    public EntityUid Source { get; }

    public NarsiSummoningStartEvent(EntityUid source)
    {
        Source = source;
    }
}

public sealed class NarsiSummoningCanceledEvent : CancellableEntityEventArgs
{
    public EntityUid Source { get; }

    public NarsiSummoningCanceledEvent(EntityUid source)
    {
        Source = source;
    }
}

public sealed class NarsiSummoningEndEvent : CancellableEntityEventArgs
{
    public EntityUid Source { get; }
    public EntityUid NarsiUid {get; }

    public NarsiSummoningEndEvent(EntityUid source, EntityUid narsiUid)
    {
        Source = source;
        NarsiUid = narsiUid;
    }
}
