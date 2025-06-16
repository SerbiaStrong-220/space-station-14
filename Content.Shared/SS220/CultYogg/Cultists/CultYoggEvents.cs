// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.CultYogg.Altar;

namespace Content.Shared.SS220.CultYogg.Cultists;

/// <summary>
///     Event raised on entities when the stage of the cult is changed.
/// </summary>
[ByRefEvent, Serializable]
public sealed class ChangeCultYoggStageEvent(CultYoggStage stage) : EntityEventArgs
{
    public CultYoggStage Stage = stage;

    public bool Handled = false;
}

[ByRefEvent, Serializable]
public sealed class CultYoggDeCultingEvent(EntityUid entity) : EntityEventArgs
{
    public readonly EntityUid Entity = entity;
}

[ByRefEvent, Serializable]
public record struct CultYoggForceAscendingEvent;

[ByRefEvent, Serializable]
public record struct CultYoggAscendingEvent;


[ByRefEvent, Serializable]
public record struct CultYoggSacrificedTargetEvent(Entity<CultYoggAltarComponent> Altar);
