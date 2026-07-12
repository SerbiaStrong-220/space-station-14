using System.Numerics;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using Content.Shared.ActionBlocker;
using Content.Shared.Disposal.Components;
using Content.Shared.Emoting;
using Content.Shared.Eye;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.SS220.Felinid.Components;
using Content.Shared.Storage.Events;
using Content.Shared.Tag;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Felinid;

[Virtual]
public partial class FelinidDashSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FelinidDashComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<FelinidDashComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnMapInit(Entity<FelinidDashComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.Action);
    }

    private void OnShutdown(Entity<FelinidDashComponent> ent, ref ComponentShutdown args)
    {
        if (Terminating(ent.Owner))
            return;

        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

}
