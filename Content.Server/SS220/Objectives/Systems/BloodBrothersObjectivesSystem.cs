using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Roles;
using Content.Shared.SS220.BloodBrothers;
using Content.Shared.SS220.Roles;

namespace Content.Server.SS220.Objectives.Systems;

public sealed class BloodBrothersObjectivesSystem : EntitySystem
{
    [Dependency] private readonly SharedRoleSystem _role = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<BloodBrothersObjectiveComponent, ComponentInit>(OnAfterAssign);
        SubscribeLocalEvent<BloodBrothersObjectiveComponent, ObjectiveProgressModifyEvent>(OnProgressModify);
    }

    private void OnAfterAssign(Entity<BloodBrothersObjectiveComponent> ent, ref ComponentInit args)
    {
        if (!TryComp<ObjectiveComponent>(ent, out var objective))
            return;

        objective.Issuer = ent.Comp.Issuer;
    }

    private void OnProgressModify(Entity<BloodBrothersObjectiveComponent> ent, ref ObjectiveProgressModifyEvent args)
    {
        if (ent.Comp.BrotherObjective is not {} brotherObjective)
            return;

        if (!_role.MindHasRole<BloodBrothersRoleComponent>(args.Mind.Owner, out var role))
            return;

        var brother = role.Value.Comp2.Brother;
        if (brother == null)
            return;

        if (!TryComp<MindComponent>(brother.Value, out var mindBroComp))
            return;

        var brotherEv = new ObjectiveGetProgressEvent(brother.Value, mindBroComp);
        RaiseLocalEvent(brotherObjective, ref brotherEv);
        args.Progress = Math.Max(args.Progress, brotherEv.Progress ?? 0f);
    }
}
