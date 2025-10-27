// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience.Systems;

public sealed partial class ExperienceSystem : EntitySystem
{
    public bool TryAddKnowledge(Entity<ExperienceComponent?> entity, ProtoId<KnowledgePrototype> knowledge)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, false))
            return false;

        if (!_prototype.Resolve(knowledge, out var knowledgePrototype))
            return false;

        entity.Comp.ConstantKnowledge.Add(knowledge);
        entity.Comp.ResolvedKnowledge.Add(knowledge);

        foreach (var additionKnowledge in knowledgePrototype.AdditionalKnowledges)
        {
            entity.Comp.ResolvedKnowledge.Add(additionKnowledge);
        }

        if (knowledgePrototype.MessageOnAcquiring is not null)
            _popup.PopupClient(Loc.GetString(knowledgePrototype.MessageOnAcquiring), entity);

        DirtyFields(entity, null, [nameof(ExperienceComponent.ConstantKnowledge), nameof(ExperienceComponent.ResolvedKnowledge)]);

        var ev = new KnowledgeGainedEvent(knowledge);
        RaiseLocalEvent(entity, ref ev);

        return true;
    }

    public bool TryRemoveKnowledge(Entity<ExperienceComponent?> entity, ProtoId<KnowledgePrototype> knowledge)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, false))
            return false;

        if (!_prototype.Resolve(knowledge, out var knowledgePrototype))
            return false;

        entity.Comp.ConstantKnowledge.Remove(knowledge);
        entity.Comp.ResolvedKnowledge.Remove(knowledge);

        foreach (var additionalKnowledgeToRemove in knowledgePrototype.AdditionalKnowledges)
        {
            entity.Comp.ResolvedKnowledge.Remove(additionalKnowledgeToRemove);
        }

        foreach (var constantKnowledge in entity.Comp.ConstantKnowledge)
        {
            if (!_prototype.Resolve(constantKnowledge, out var constantKnowledgePrototype))
                continue;

            foreach (var additionKnowledge in constantKnowledgePrototype.AdditionalKnowledges)
            {
                entity.Comp.ResolvedKnowledge.Add(additionKnowledge);
            }
        }

        if (knowledgePrototype.MessageOnLosing is not null)
            _popup.PopupClient(Loc.GetString(knowledgePrototype.MessageOnLosing), entity);

        DirtyFields(entity, null, [nameof(ExperienceComponent.ConstantKnowledge), nameof(ExperienceComponent.ResolvedKnowledge)]);

        var ev = new KnowledgeLostEvent(knowledge);
        RaiseLocalEvent(entity, ref ev);

        return true;
    }

    /// <summary>
    /// Doesn't change hashset if entity don't have <see cref="ExperienceComponent"/> </br>
    /// Return only knowledges that entity have without resolving additional knowledges
    /// </summary>
    public bool TryGetEntityKnowledge(Entity<ExperienceComponent?> entity, ref HashSet<ProtoId<KnowledgePrototype>> knowledges)
    {
        if (HasComp<BypassKnowledgeCheckComponent>(entity))
        {
            knowledges = [.. _prototype.EnumeratePrototypes<KnowledgePrototype>().Select(x => x.ID)];
            return true;
        }

        if (!Resolve(entity.Owner, ref entity.Comp, false))
            return false;

        knowledges = [.. entity.Comp.ConstantKnowledge];
        return true;
    }

    public bool HaveKnowledge(Entity<ExperienceComponent?> entity, ProtoId<KnowledgePrototype> knowledge)
    {
        if (HasComp<BypassKnowledgeCheckComponent>(entity))
            return true;

        if (!Resolve(entity.Owner, ref entity.Comp, false))
            return false;

        return entity.Comp.ResolvedKnowledge.Contains(knowledge);
    }
}
