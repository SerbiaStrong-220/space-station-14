using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.SS220.Felinid.Components;

namespace Content.Shared.SS220.Felinid;

[Virtual]
public partial class DashSpeedModifierSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DashSpeedModifierComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DashSpeedModifierComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnMapInit(Entity<DashSpeedModifierComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.Action);
    }

    private void OnShutdown(Entity<DashSpeedModifierComponent> ent, ref ComponentShutdown args)
    {
        if (Terminating(ent.Owner))
            return;

        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }
}
