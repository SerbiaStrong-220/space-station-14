// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Antag.Components;
using Content.Shared.Roles;
using Content.Shared.SS220.Experience;
using Content.Shared.SS220.Experience.Components;
using Content.Shared.SS220.Experience.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.AntagRemoval;

public sealed partial class RemoveAntagSystem
{
    private static ExperienceAddSource? ExperienceSourceForRole(string roleId) => roleId switch
    {
        RemoveAntagRoles.Traitor => ExperienceAddSource.AntagTraitor,
        RemoveAntagRoles.Thief => ExperienceAddSource.AntagThief,
        RemoveAntagRoles.Cult => ExperienceAddSource.AntagCult,
        RemoveAntagRoles.InitialInfected => ExperienceAddSource.AntagInitialInfected,
        RemoveAntagRoles.HeadRevolutionary => ExperienceAddSource.AntagHeadRevolutionary,
        _ => null
    };

    public void GrantAntagKnowledge(EntityUid body, string roleId, ProtoId<KnowledgePrototype> knowledge)
    {
        if (ExperienceSourceForRole(roleId) is not { } source || !HasComp<ExperienceComponent>(body))
            return;

        var old = GetAntagExperienceSpecifier(body, source);
        var knowledges = new HashSet<ProtoId<KnowledgePrototype>>(old.Knowledges) { knowledge };
        _experienceAddition.UpsertSpecifier(body, source,
            new ExperienceAddSpecifier(old.FreeSublevelPoints, knowledges, new(old.Skills)));
    }

    public void GrantAntagBonuses(EntityUid body, string roleId, int points,
        params ProtoId<KnowledgePrototype>[] knowledges)
    {
        if (ExperienceSourceForRole(roleId) is not { } source || !HasComp<ExperienceComponent>(body))
            return;

        var old = GetAntagExperienceSpecifier(body, source);
        var granted = new HashSet<ProtoId<KnowledgePrototype>>(old.Knowledges);
        granted.UnionWith(knowledges);
        _experienceAddition.UpsertSpecifier(body, source,
            new ExperienceAddSpecifier(points, granted, new(old.Skills)));
    }

    public void RegisterGrantExperience(EntityUid body, AntagSelectionDefinition definition)
    {
        if (definition.StartingGear is not { } gearId ||
            !_prototypes.TryIndex(gearId, out StartingGearPrototype? gear) ||
            gear.ExperienceDefinition is not { } expId ||
            !_prototypes.TryIndex(expId, out ExperienceDefinitionPrototype? expDef))
            return;

        // The primary definition already accounts for this gear's experience.
        if (TryComp<ExperienceAdditionComponent>(body, out var addition) && addition.DefinitionId == expId)
            return;

        foreach (var role in definition.MindRoles ?? new())
        {
            if (ExperienceSourceForRole(role.Id) is not { } source)
                continue;

            var old = GetAntagExperienceSpecifier(body, source);
            var knowledges = new HashSet<ProtoId<KnowledgePrototype>>(old.Knowledges);
            knowledges.UnionWith(expDef.Knowledges);
            var skills = new Dictionary<ProtoId<SkillTreePrototype>, SkillTreeInfo>(old.Skills);
            foreach (var (skill, info) in expDef.Skills)
            {
                if (skills.TryGetValue(skill, out var existing))
                {
                    var combined = new SkillTreeInfo(existing);
                    combined.Add(info);
                    skills[skill] = combined;
                }
                else
                    skills[skill] = new SkillTreeInfo(info);
            }

            _experienceAddition.UpsertSpecifier(body, source,
                new ExperienceAddSpecifier(old.FreeSublevelPoints + expDef.AddSublevelPoints, knowledges, skills));
        }
    }

    private ExperienceAddSpecifier GetAntagExperienceSpecifier(EntityUid body, ExperienceAddSource source)
        => TryComp<ExperienceAdditionComponent>(body, out var addition) &&
           addition.Sources.TryGetValue(source, out var specifier)
            ? specifier : new ExperienceAddSpecifier();

    private int GetRoleBonusPoints(EntityUid body, string roleId)
        => ExperienceSourceForRole(roleId) is { } source
            ? GetAntagExperienceSpecifier(body, source).FreeSublevelPoints
            : 0;

    private bool CanRevokeAntagExperience(EntityUid body, string roleId, out string reason)
    {
        reason = string.Empty;

        if (!TryComp<ExperienceComponent>(body, out var exp))
            return true;

        var deficit = Math.Max(0, GetRoleBonusPoints(body, roleId) - exp.FreeSublevelPoints);
        if (deficit == 0)
            return true;

        if (TryComp<JobBackgroundSublevelAddComponent>(body, out var spent))
        {
            var remainingSkills = new Dictionary<ProtoId<SkillTreePrototype>, int>(spent.Skills);
            var refundable = 0;
            for (var i = spent.RemoveAntagPurchases.Count - 1; i >= 0 && refundable < deficit; i--)
            {
                var purchase = spent.RemoveAntagPurchases[i];
                if (!remainingSkills.TryGetValue(purchase.SkillTree, out var held))
                    continue;

                var available = Math.Min(purchase.Amount, held);
                refundable += available;
                remainingSkills[purchase.SkillTree] -= available;
            }

            if (refundable >= deficit)
                return true;
        }

        // Previous allocations without a purchase history cannot be safely rolled back.
        reason = Loc.GetString("ss220-removeantag-error-unavailable");
        return false;
    }

    private void RecalculateExperience(EntityUid body, JobPrototype job, string removedRole, int lostPoints)
    {
        if (!TryComp<ExperienceComponent>(body, out var exp))
            return;

        var deficit = Math.Max(0, lostPoints - exp.FreeSublevelPoints);
        if (deficit > 0 && TryComp<JobBackgroundSublevelAddComponent>(body, out var purchases))
        {
            for (var i = purchases.RemoveAntagPurchases.Count - 1; i >= 0 && deficit > 0; i--)
            {
                var purchase = purchases.RemoveAntagPurchases[i];
                if (!purchases.Skills.TryGetValue(purchase.SkillTree, out var held))
                    continue;

                var refunded = Math.Min(deficit, Math.Min(purchase.Amount, held));
                if (refunded <= 0)
                    continue;

                held -= refunded;
                if (held == 0)
                    purchases.Skills.Remove(purchase.SkillTree);
                else
                    purchases.Skills[purchase.SkillTree] = held;

                purchase.Amount -= refunded;
                if (purchase.Amount == 0)
                    purchases.RemoveAntagPurchases.RemoveAt(i);

                purchases.SpentSublevelPoints -= refunded;
                deficit -= refunded;
            }

            Dirty(body, purchases);
        }

        if (ExperienceSourceForRole(removedRole) is { } source)
            _experienceAddition.UpsertSpecifier(body, source, null);

        var changeDef = new TryChangeExperienceDefinitionEvent(job.ExperienceDefinition, true);
        RaiseLocalEvent(body, ref changeDef);
        _experienceAddition.EnqueueRecalculation(body);
    }
}
