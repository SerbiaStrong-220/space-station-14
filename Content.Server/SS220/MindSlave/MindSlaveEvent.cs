// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

/// <summary>
/// Event raised right after Mindslave component added to Entity on slave entity
/// </summary>
public sealed class AfterEntityMindSlavedEvent(EntityUid master, EntityUid slave) : EventArgs
{
    public EntityUid Master { get; } = master;
    public EntityUid Slave { get; } = slave;
}

/// <summary>
/// Event raised right after Mindslave component added to Entity om master entity
/// </summary>
public sealed class AfterEntityMindSlavedMasterEvent(EntityUid master, EntityUid slave) : EventArgs
{
    public EntityUid Master { get; } = master;
    public EntityUid Slave { get; } = slave;
}

public sealed class StopWordGeneratedEvent(string stopWord) : EventArgs
{
    public string StopWord { get; } = stopWord;
}
