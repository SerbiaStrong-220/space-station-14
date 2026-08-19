// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Shuttles.Systems;
using Content.Server.SS220.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles;
using Content.Shared.SS220.Roles;

namespace Content.Server.SS220.Objectives.Systems;

public sealed partial class BloodBrothersEscapeShuttleConditionSystem : EntitySystem
{
    [Dependency] private EmergencyShuttleSystem _emergency = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedRoleSystem _role = default!;

    private const float Quarter = 0.25f;

    public override void Initialize()
    {
        SubscribeLocalEvent<BloodBrothersEscapeShuttleConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(Entity<BloodBrothersEscapeShuttleConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = 0f;

        if (!_role.MindHasRole<BloodBrothersRoleComponent>(args.MindId, out var role))
            return;

        if (args.Mind.OwnedEntity is { } currentEntity)
        {
            if (_mobState.IsAlive(currentEntity))
                args.Progress += Quarter;

            if (_emergency.IsTargetEscaping(currentEntity))
                args.Progress += Quarter;
        }

        if (role.Value.Comp2.Brother is not { } brother)
            return;

        if (!TryComp<MindComponent>(brother, out var brotherMind) || brotherMind.OwnedEntity == null)
            return;

        if (_emergency.IsTargetEscaping(brotherMind.OwnedEntity.Value))
            args.Progress += Quarter;

        if (_mobState.IsAlive(brotherMind.OwnedEntity.Value))
            args.Progress += Quarter;
    }
}
