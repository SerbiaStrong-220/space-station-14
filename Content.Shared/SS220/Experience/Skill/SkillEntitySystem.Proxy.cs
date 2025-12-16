// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience.Skill;

public partial class SkillEntitySystem : EntitySystem
{
    /// <summary>
    /// Subscribes Experience component and its handlers to specified TComp TEvent handler
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void SubscribeEventToSkillEntity<TComp, TEvent>(EntityEventRefHandler<TComp, TEvent> handler,
                                                        Type[]? before = null, Type[]? after = null)
                                                        where TEvent : notnull where TComp : Component
    {
        Experience.RelayEventToSkillEntity<TComp, TEvent>();

        SubscribeLocalEvent<TComp, TEvent>(handler, before, after);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ResolveExperienceEntityFromSkillEntity(EntityUid uid, [NotNullWhen(true)] out Entity<ExperienceComponent>? experienceEntity)
    {
        return Experience.ResolveExperienceEntityFromSkillEntity(uid, out experienceEntity);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryChangeStudyingProgress(EntityUid uid, ProtoId<SkillTreePrototype> skillTree, LearningInformation info)
    {
        if (!Experience.ResolveExperienceEntityFromSkillEntity(uid, out var experienceEntity))
            return false;

        return Experience.TryChangeStudyingProgress(experienceEntity.Value!, skillTree, info);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryChangeStudyingProgress(EntityUid uid, ProtoId<SkillTreePrototype> skillTree, FixedPoint4 delta)
    {
        if (!Experience.ResolveExperienceEntityFromSkillEntity(uid, out var experienceEntity))
            return false;

        return Experience.TryChangeStudyingProgress(experienceEntity.Value!, skillTree, delta);
    }

    /// <summary>
    /// Resolves owner entity of skill and writes log with adding parent entity
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAddToAdminLogs<T>(Entity<T> entity, string message, LogImpact logImpact = LogImpact.Low) where T : IComponent
    {
        if (!ResolveExperienceEntityFromSkillEntity(entity, out var experienceEntity))
            return false;

        _adminLog.Add(LogType.Experience, logImpact, $"Skill of {ToPrettyString(experienceEntity):user} caused {message}");
        return true;
    }

    /// <summary>
    /// Random that gives same result on client and on server
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public System.Random GetPredictedRandom(in List<int> valuesForSeed)
    {
        var toCombine = new List<int>(valuesForSeed);
        toCombine.Add((int)GameTiming.CurTick.Value);

        var seed = SharedRandomExtensions.HashCodeCombine(toCombine);
        return new System.Random(seed);
    }
}
