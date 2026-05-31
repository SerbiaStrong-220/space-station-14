using System.Linq;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Server.SS220.GameTicking.Rules.Components;
using Content.Shared.Mind;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Players;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.SS220.BloodBrothers;
using Content.Shared.SS220.Roles;
using Robust.Shared.Prototypes;
// ReSharper disable InvertIf

namespace Content.Server.SS220.GameTicking.Rules;

public sealed class BloodBrothersRuleSystem : GameRuleSystem<BloodBrothersRuleComponent>
{
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly SharedRoleSystem _role = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;

    private static readonly ProtoId<NpcFactionPrototype> SyndicateFaction = "Syndicate";
    private static readonly ProtoId<NpcFactionPrototype> NanoTrasenFaction = "NanoTrasen";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BloodBrothersRuleComponent, AfterAntagEntitySelectedEvent>(AfterAntagSelected, after: [typeof(AntagRandomObjectivesSystem)]);
        SubscribeLocalEvent<BloodBrothersRoleComponent, GetBriefingEvent>(OnGetBriefing);
    }

    private void AfterAntagSelected(Entity<BloodBrothersRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        var mind = args.Session?.GetMind();
        if (mind is null)
            return;

        if (!_role.MindHasRole<BloodBrothersRoleComponent>(mind.Value, out var role))
            return;

        if (!TryComp<MindComponent>(mind.Value, out var mindComp))
            return;

        _npcFaction.RemoveFaction(args.EntityUid, NanoTrasenFaction);
        _npcFaction.AddFaction(args.EntityUid, SyndicateFaction);

        var briefing = args.GameRule.Comp.Definitions.First().Briefing;
        if (args.GameRule.Comp.AssignedSessions.Count < 2)
            return;

        var firstBrotherMind = args.GameRule.Comp.AssignedSessions.First().GetMind();
        if (firstBrotherMind == null || firstBrotherMind == mind)
            return;

        if (!_role.MindHasRole<BloodBrothersRoleComponent>(firstBrotherMind.Value, out var firstBrotherRole))
            return;

        if (!TryComp<MindComponent>(firstBrotherMind.Value, out var firstBrotherMindComp))
            return;

        firstBrotherRole.Value.Comp2.Brother = mind.Value;

        CopyObjectives(firstBrotherMind.Value, mind.Value);

        role.Value.Comp2.Brother = firstBrotherMind.Value;

        if (firstBrotherMindComp.OwnedEntity != null && mindComp.CharacterName != null && briefing != null)
        {
            _antag.SendBriefing(firstBrotherMindComp.OwnedEntity.Value,
                MakeBriefing(firstBrotherMind.Value),
                null,
                briefing.Value.Sound);
        }

        if (mindComp.OwnedEntity != null && firstBrotherMindComp.CharacterName != null && briefing != null)
        {
            _antag.SendBriefing(mindComp.OwnedEntity.Value,
                MakeBriefing(mind.Value),
                null,
                briefing.Value.Sound);
        }
    }

    private void OnGetBriefing(Entity<BloodBrothersRoleComponent> role, ref GetBriefingEvent args)
    {
        var briefing = MakeBriefing(args.Mind);
        if (!string.IsNullOrEmpty(briefing))
            args.Append("\n" + briefing);
    }

    private string MakeBriefing(EntityUid userMind)
    {
        if (!_role.MindHasRole<BloodBrothersRoleComponent>(userMind, out var userRole))
            return string.Empty;

        var brother = userRole.Value.Comp2.Brother;
        if (brother == null || !TryComp<MindComponent>(brother.Value, out var brotherMindComp) ||
            brotherMindComp.CharacterName == null)
            return string.Empty;

        var jobName = _job.MindTryGetJobName(brother.Value);
        var briefing = Loc.GetString("blood-brothers-role-greeting",
            ("brotherName", brotherMindComp.CharacterName),
            ("brotherJobName", jobName));

        return briefing;
    }

    private void CopyObjectives(Entity<MindComponent?> mindUser, Entity<MindComponent?> mindTarget)
    {
        if (!Resolve(mindUser.Owner, ref mindUser.Comp) || !Resolve(mindTarget.Owner, ref mindTarget.Comp))
            return;

        mindUser.Comp.Objectives.Clear();
        _mind.CopyObjectives(mindTarget.Owner, mindUser.Owner);

        var userObjectives = mindUser.Comp.Objectives;
        var targetObjectives = mindTarget.Comp.Objectives;

        // here we need to change issuer on comp init
        foreach (var objective in userObjectives)
        {
            EnsureComp<BloodBrothersObjectiveComponent>(objective);
        }

        foreach (var objective in targetObjectives)
        {
            EnsureComp<BloodBrothersObjectiveComponent>(objective);
        }

        var count = Math.Min(userObjectives.Count, targetObjectives.Count);
        for (var i = 1; i < count; i++) // we skip the first obj, cause this escape obj
        {
            var userObj = userObjectives[i];
            var targetObj = targetObjectives[i];

            // assign brother mind entity to objective to sync objectives
            if (TryComp<BloodBrothersObjectiveComponent>(userObj, out var userBro))
            {
                userBro.BrotherObjective = targetObj;
                Dirty(userObj, userBro);
            }

            if (TryComp<BloodBrothersObjectiveComponent>(targetObj, out var targetBro))
            {
                targetBro.BrotherObjective = userObj;
                Dirty(targetObj, targetBro);
            }
        }
    }
}
