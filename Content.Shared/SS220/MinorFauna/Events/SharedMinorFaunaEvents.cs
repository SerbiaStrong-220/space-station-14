using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.MinorFauna.Events;

[Serializable, NetSerializable]
public sealed partial class AfterEntityCocooningEvent : SimpleDoAfterEvent
{
}
