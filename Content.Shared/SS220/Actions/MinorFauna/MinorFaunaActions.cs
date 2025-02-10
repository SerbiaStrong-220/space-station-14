using Content.Shared.Actions;
using Robust.Shared.Serialization;
using Content.Shared.DoAfter;

namespace Content.Shared.SS220.MinorFauna;

//Actions
public sealed partial class ActionSpiderCocooningEvent : EntityTargetActionEvent
{
    [DataField]
    public TimeSpan CocooningTime = TimeSpan.Zero;
}

//DoAFters
[Serializable, NetSerializable]
public sealed partial class AfterSpiderCocooningEvent : SimpleDoAfterEvent
{

}
