namespace Content.Server.SS220.DarkForces.Saint.Chaplain.Abilities;

public sealed partial class ChaplainNarsiExileEnableEvent : CancellableEntityEventArgs
{
    public EntityUid Chaplain { get; }

    public ChaplainNarsiExileEnableEvent(EntityUid chaplain)
    {
        Chaplain = chaplain;
    }
}


public sealed partial class ChaplainNarsiExileStartEvent : CancellableEntityEventArgs
{
    public EntityUid Chaplain { get; }

    public ChaplainNarsiExileStartEvent(EntityUid chaplain)
    {
        Chaplain = chaplain;
    }
}

public sealed partial class ChaplainNarsiExileCanceledEvent : CancellableEntityEventArgs
{
    public EntityUid Chaplain { get; }

    public ChaplainNarsiExileCanceledEvent(EntityUid chaplain)
    {
        Chaplain = chaplain;
    }
}


public sealed partial class ChaplainNarsiExileFinishedEvent : CancellableEntityEventArgs
{
    public EntityUid Chaplain { get; }

    public ChaplainNarsiExileFinishedEvent(EntityUid chaplain)
    {
        Chaplain = chaplain;
    }
}
