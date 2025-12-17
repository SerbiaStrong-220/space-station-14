// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience;

/// <summary>
/// Event raised on entity when <see cref="SkillTreePrototype"/> added to recalculate <see cref="ExperienceComponent.Skills"/>
/// </summary>
/// <param name="SkillTree"> Id of added <see cref="SkillTreePrototype"/> </param>
/// <param name="Info"> This struct contains additions to start level, all higher than max level will be correctly added </param>
/// <param name="DenyChanges"> Setting it to true prevent next changes, think of order before using </param>
[ByRefEvent]
public record struct SkillTreeAdded(ProtoId<SkillTreePrototype> SkillTree, SkillTreeInfo Info, bool DenyChanges = false);

/// <summary>
/// Event raised on entity when experience component being recalculated to collect knowledge
/// </summary>
/// <param name="Knowledges"> Collection which contains knowledges </param>
/// <param name="DenyChanges"> Setting it to true prevent next changes, think of order before using </param>
[ByRefEvent]
public record struct KnowledgeInitialResolve(HashSet<ProtoId<KnowledgePrototype>> Knowledges, bool DenyChanges = false);

/// <summary>
/// Event raised on entity when experience component being recalculated to free sublevel point
/// </summary>
/// <param name="FreeSublevelPoints"> Contains result of adding by subscribed components </param>
/// <param name="DenyChanges"> Setting it to true prevent next changes, think of order before using </param>
[ByRefEvent]
public record struct SublevelAdditionPointInitialResolve(int FreeSublevelPoints, bool DenyChanges = false);

/// <summary>
/// Event raised on entity which gained skill
/// </summary>
[ByRefEvent]
public record struct SkillLevelGainedEvent(ProtoId<SkillTreePrototype> SkillTree, ProtoId<SkillPrototype> GainedSkill);

/// <summary>
/// Event raised on entity which gained knowledge
/// </summary>
[ByRefEvent]
public record struct KnowledgeGainedEvent(ProtoId<KnowledgePrototype> Knowledge);

/// <summary>
/// Event raised on entity which lost knowledge
/// </summary>
[ByRefEvent]
public record struct KnowledgeLostEvent(ProtoId<KnowledgePrototype> Knowledge);

/// <summary>
/// Raised on override skill entity to check if it subscribed to event
/// </summary>
/// <typeparam name="T"> event type </typeparam>
/// <param name="Subscribed"> flag that returned if entity subscribed </param>
[ByRefEvent]
public record struct SkillEntityOverrideCheckEvent<T>(bool Subscribed = false) where T : notnull;
