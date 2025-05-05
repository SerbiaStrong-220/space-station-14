// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Server.SS220.SuicideByGun;

[Serializable, NetSerializable]
public sealed partial class SuicideDoAfterEvent : SimpleDoAfterEvent
{
//    public SuicideDoAfterEvent() {}
//
//    public override DoAfterEvent Clone() => new SuicideDoAfterEvent();
//    public override bool IsDuplicate(DoAfterEvent other) => false;
}
