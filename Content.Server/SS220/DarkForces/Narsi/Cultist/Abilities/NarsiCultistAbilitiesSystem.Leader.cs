using Content.Server.SS220.DarkForces.Narsi.Buildings.Altar.Rituals.Polymorth;
using Content.Server.Polymorph.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.SS220.DarkForces.Narsi.Abilities.Events;
using Content.Shared.SS220.DarkForces.Narsi.Roles;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.SS220.DarkForces.Narsi.Cultist.Abilities;

public sealed partial class NarsiCultistAbilitiesSystem
{
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;

    private void InitializeLeaderAbilities()
    {
        SubscribeLocalEvent<NarsiCultistLeaderComponent, ComponentInit>(OnLeaderInit);
        SubscribeLocalEvent<NarsiCultistLeaderComponent, ComponentShutdown>(OnLeaderShutdown);
        SubscribeLocalEvent<NarsiCultistLeaderComponent, NarsiCultistLeaderEvent>(OnCultistLeaderAbility);
    }

    private void OnCultistLeaderAbility(EntityUid uid, NarsiCultistLeaderComponent component,
        NarsiCultistLeaderEvent args)
    {
        if (args.Handled)
            return;

        var leaderTransform = Transform(uid);
        var query = EntityQueryEnumerator<NarsiCultistComponent>();
        while (query.MoveNext(out var cultist, out _))
        {
            if (cultist == uid || !_mobStateSystem.IsAlive(cultist))
                continue;

            var cultistTransform = Transform(cultist);
            if (leaderTransform.MapID != cultistTransform.MapID)
                continue;

            _transformSystem.SetCoordinates(cultist, cultistTransform, leaderTransform.Coordinates);
            _transformSystem.AttachToGridOrMap(uid);
        }

        OnCultistAbility(uid, args);
        args.Handled = true;
    }

    private void OnLeaderInit(EntityUid uid, NarsiCultistLeaderComponent component, ComponentInit args)
    {
        _actionsSystem.AddAction(uid, ref component.LeaderAbilityEntityUid, component.LeaderAbility);
    }

    private void OnLeaderShutdown(EntityUid uid, NarsiCultistLeaderComponent component, ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(uid, component.LeaderAbilityEntityUid);
    }
}
