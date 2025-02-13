using Content.Shared.Actions;

namespace Content.Shared.SS220.MinorFauna.Actions;

//Actions
public sealed partial class ActionEntityCocooningEvent : EntityTargetActionEvent
{
    [DataField]
    public TimeSpan CocooningTime = TimeSpan.Zero;
}
