using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Antag;
using Content.Server.Antag.Components;
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
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
// ReSharper disable InvertIf

namespace Content.Server.SS220.GameTicking.Rules;

public sealed partial class BloodBrothersRuleSystem : GameRuleSystem<BloodBrothersRuleComponent>
{
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private SharedRoleSystem _role = default!;
    [Dependency] private NpcFactionSystem _npcFaction = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private SharedJobSystem _job = default!;

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
        if (!TryGetAntagData(args, out var mind, out var role, out var mindComp))
            return;

        var assignedSessions = args.GameRule.Comp.AssignedSessions;

        InitializeAntagFaction(args.EntityUid);
        if (assignedSessions.Count < 2)
            return;

        TryLinkBloodBrothers(assignedSessions, (mind.Value, mindComp), role.Value, args.Def.Briefing);
    }

    private bool TryGetAntagData(
        AfterAntagEntitySelectedEvent args,
        [NotNullWhen(true)] out EntityUid? mind,
        [NotNullWhen(true)] out Entity<BloodBrothersRoleComponent>? role,
        [NotNullWhen(true)] out MindComponent? mindComp)
    {
        mind = null;
        role = null;
        mindComp = null;

        var sessionMind = args.Session?.GetMind();
        if (sessionMind is null)
            return false;

        if (!_role.MindHasRole<BloodBrothersRoleComponent>(sessionMind.Value, out var foundRole))
            return false;

        if (!TryComp<MindComponent>(sessionMind.Value, out var comp))
            return false;

        mind = sessionMind.Value;
        role = (foundRole.Value.Owner, foundRole.Value.Comp2);
        mindComp = comp;
        return true;
    }

    private void InitializeAntagFaction(EntityUid entityUid)
    {
        _npcFaction.RemoveFaction(entityUid, NanoTrasenFaction);
        _npcFaction.AddFaction(entityUid, SyndicateFaction);
    }

    private void TryLinkBloodBrothers(
        HashSet<ICommonSession> assignedSessions,
        Entity<MindComponent> currentMind,
        Entity<BloodBrothersRoleComponent> currentRole,
        BriefingData? briefing)
    {
        var firstBrotherSession = assignedSessions.FirstOrDefault();
        var firstBrotherMind = firstBrotherSession?.GetMind();

        if (firstBrotherMind == null || firstBrotherMind == currentMind)
            return;

        if (!_role.MindHasRole<BloodBrothersRoleComponent>(firstBrotherMind.Value, out var firstBrotherRole))
            return;

        if (!TryComp<MindComponent>(firstBrotherMind.Value, out var firstBrotherMindComp))
            return;

        firstBrotherRole.Value.Comp2.Brother = currentMind;
        currentRole.Comp.Brother = firstBrotherMind.Value;

        CopyObjectives(firstBrotherMind.Value, currentMind.Owner);

        Dirty(firstBrotherRole.Value);
        Dirty(currentRole);

        if (briefing != null)
        {
            if (firstBrotherMindComp.OwnedEntity != null && currentMind.Comp.CharacterName != null)
            {
                _antag.SendBriefing(firstBrotherMindComp.OwnedEntity.Value,
                    MakeBriefing(firstBrotherMind.Value),
                    null,
                    briefing.Value.Sound);
            }

            if (currentMind.Comp.OwnedEntity != null && firstBrotherMindComp.CharacterName != null)
            {
                _antag.SendBriefing(currentMind.Comp.OwnedEntity.Value,
                    MakeBriefing(currentMind),
                    null,
                    briefing.Value.Sound);
            }
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
        {
            return string.Empty;
        }

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

        foreach (var objective in mindUser.Comp.Objectives)
        {
            _mind.TryRemoveObjective(mindUser.Owner, mindUser.Comp, objective);
        }

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
