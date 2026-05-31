using Content.Server.Shuttles.Systems;
using Content.Server.SS220.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles;
using Content.Shared.SS220.Roles;

namespace Content.Server.SS220.Objectives.Systems;

public sealed class BloodBrothersEscapeShuttleConditionSystem : EntitySystem
{
    [Dependency] private readonly EmergencyShuttleSystem _emergency = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BloodBrothersEscapeShuttleConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(Entity<BloodBrothersEscapeShuttleConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = 0f;
        if (!_role.MindHasRole<BloodBrothersRoleComponent>(args.MindId, out var role))
            return;

        var currentEntity = args.Mind.OwnedEntity;
        if (currentEntity == null)
            return;

        var mobState = EntityManager.System<MobStateSystem>();
        if (mobState.IsAlive(currentEntity.Value))
            args.Progress += 0.25f;

        if (_emergency.IsTargetEscaping(currentEntity.Value))
            args.Progress += 0.25f;

        var brother = role.Value.Comp2.Brother;
        if (brother == null)
            return;

        if (!TryComp<MindComponent>(brother.Value, out var brotherMind) || brotherMind.OwnedEntity == null)
            return;

        if (_emergency.IsTargetEscaping(brotherMind.OwnedEntity.Value))
            args.Progress += 0.25f;

        if (mobState.IsAlive(brotherMind.OwnedEntity.Value))
            args.Progress += 0.25f;
    }
}
