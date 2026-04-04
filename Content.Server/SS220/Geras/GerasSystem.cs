using Content.Server.Polymorph.Systems;
using Content.Shared.Zombies;
using Content.Server.Actions;
using Content.Server.Popups;
using Content.Shared.SS220.Geras;
using Robust.Shared.Player;

namespace Content.Server.SS220.Geras;

/// <inheritdoc/>
public sealed class GerasSystem : SharedGerasSystem
{
    [Dependency] private readonly PolymorphSystem _polymorphSystem = default!;
    [Dependency] private readonly ActionsSystem _actionsSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<GerasComponent, MorphIntoGeras>(OnMorphIntoGeras);
        SubscribeLocalEvent<GerasComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<GerasComponent, EntityZombifiedEvent>(OnZombification);
    }

    private void OnZombification(Entity<GerasComponent> ent, ref EntityZombifiedEvent args)
    {
        _actionsSystem.RemoveAction(ent.Owner, ent.Comp.GerasActionEntity);
    }

    private void OnMapInit(Entity<GerasComponent> ent, ref MapInitEvent args)
    {
        // try to add geras action
        _actionsSystem.AddAction(ent.Owner, ref ent.Comp.GerasActionEntity, ent.Comp.GerasAction);
    }

    private void OnMorphIntoGeras(Entity<GerasComponent> ent, ref MorphIntoGeras args)
    {
        if (HasComp<ZombieComponent>(ent.Owner))
            return; // i hate zomber.

        var entity = _polymorphSystem.PolymorphEntity(ent.Owner, ent.Comp.GerasPolymorphId);

        if (!entity.HasValue)
            return;

        _popupSystem.PopupEntity(Loc.GetString("geras-popup-morph-message-others", ("entity", entity.Value)), entity.Value, Filter.PvsExcept(entity.Value), true);
        _popupSystem.PopupEntity(Loc.GetString("geras-popup-morph-message-user"), entity.Value, entity.Value);

        args.Handled = true;
    }
}
