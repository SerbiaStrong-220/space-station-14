namespace Content.Shared.SS220.CultYogg.Cultists;

/// <summary>
///     Event raised on entities when the stage of the cult is changed.
/// </summary>
[ByRefEvent, Serializable]
public sealed class ChangeCultYoggStageEvent : EntityEventArgs
{
    public int Stage;
    public ChangeCultYoggStageEvent(int stage)
    {
        Stage = stage;
    }
}

[ByRefEvent, Serializable]
public record struct CultYoggDeleteVisualsEvent;


[ByRefEvent, Serializable]
public sealed class CultYoggDeCultingEvent : EntityEventArgs
{
    public readonly EntityUid Entity;

    public CultYoggDeCultingEvent(EntityUid entity)
    {
        Entity = entity;
    }
}

[ByRefEvent, Serializable]
public record struct CultYoggForceAscendingEvent;

