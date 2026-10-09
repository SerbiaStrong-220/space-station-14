// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.StuckOnEquip;

[Serializable, NetSerializable]
public sealed partial class CutStuckDoAfterEvent : DoAfterEvent
{
    /// <summary>
    /// Original equipment container; cutting stops if the item moves to a different container.
    /// </summary>
    public readonly string ContainerId;

    public CutStuckDoAfterEvent(string containerId)
    {
        ContainerId = containerId;
    }

    public override DoAfterEvent Clone()
    {
        return this;
    }
}
