// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Network;

namespace Content.Server.SS220.CultYogg.Sacraficials;

[ByRefEvent, Serializable]
public sealed class SacraficialReplacementEvent : EntityEventArgs
{
    public readonly EntityUid Entity;
    public readonly NetUserId Player;

    public SacraficialReplacementEvent(EntityUid entity, NetUserId player)
    {
        Entity = entity;
        Player = player;
    }
}
