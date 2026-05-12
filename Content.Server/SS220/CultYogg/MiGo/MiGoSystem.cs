// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Roles.Jobs;
using Content.Server.SS220.GameTicking.Rules;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Mind.Components;
using Content.Shared.SS220.CultYogg.MiGo;
using Content.Shared.StatusEffect;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.CultYogg.MiGo;

public sealed partial class MiGoSystem : SharedMiGoSystem
{
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
    [Dependency] private readonly StomachSystem _stomach = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly JobSystem _jobSystem = default!;
    [Dependency] private readonly CultYoggRuleSystem _cultRuleSystem = default!;

    private readonly ProtoId<ReagentPrototype> _ascensionReagent = "TheBloodOfYogg";

    public override void Initialize()
    {
        base.Initialize();

        //actions
        SubscribeLocalEvent<MiGoComponent, MiGoEnslaveDoAfterEvent>(MiGoEnslaveOnDoAfter);
        SubscribeLocalEvent<MiGoComponent, MindAddedMessage>(OnMindAdded);
    }

    protected override void SyncStage(Entity<MiGoComponent> ent)
    {
        if (!_cultRuleSystem.TryGetCultGameRule(out var rule))
            return;

        ent.Comp.CurrentStage = rule.Value.Comp.Stage;
        Dirty(ent);
    }

    private void OnMindAdded(Entity<MiGoComponent> ent, ref MindAddedMessage args)
    {
        _jobSystem.MindAddJob(args.Mind, ent.Comp.JobName);
    }

    #region Enslave
    private void MiGoEnslaveOnDoAfter(Entity<MiGoComponent> uid, ref MiGoEnslaveDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null)
            return;

        args.Handled = true;

        var ev = new CultYoggEnslavedEvent(args.Target);
        RaiseLocalEvent(uid, ref ev, true);

        _statusEffectsSystem.TryRemoveStatusEffect(args.Target.Value, uid.Comp.RequiedEffect); //Remove Rave cause he already cultist

        if (!TryComp<BodyComponent>(args.Target.Value, out var body))
            return;

        if (body.Organs == null)
            return;

        foreach (var organ in body.Organs.ContainedEntities)
        {
            if (!TryComp<StomachComponent>(organ, out var stomachComp))
                continue;

            if (stomachComp.Solution == null)
                continue;

            var reagentToRemove = new ReagentQuantity(_ascensionReagent, FixedPoint2.MaxValue);
            _stomach.TryRemoveReagent(organ, reagentToRemove);
            _solutionContainer.RemoveReagent(stomachComp.Solution.Value, _ascensionReagent, reagentToRemove.Quantity);
        }
    }
    #endregion
}
