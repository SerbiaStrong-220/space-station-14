// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Server.SS220.CultYogg.Sacrificials;

[ByRefEvent, Serializable]
public sealed class SacrificialReplacementEvent(EntityUid entity) : EntityEventArgs
{
    public readonly EntityUid Entity = entity;
}
