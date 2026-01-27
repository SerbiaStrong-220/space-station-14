// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

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
    public bool ResolveExperienceEntityFromSkillEntity(EntityUid skillUid, [NotNullWhen(true)] out Entity<ExperienceComponent>? experienceEntity)
    {
        experienceEntity = null;

        DebugTools.AssertEqual(HasComp<SkillComponent>(skillUid), true);
        DebugTools.AssertEqual(_container.IsEntityInContainer(skillUid), true, $"Got entity {ToPrettyString(skillUid)} with {nameof(SkillComponent)} but not in container");

        var experienceUid = Transform(skillUid).ParentUid;

        if (!TryComp<ExperienceComponent>(experienceUid, out var experienceComponent))
        {
            Log.Error($"Got entity {ToPrettyString(experienceUid)} in container which entity owner don't have {nameof(ExperienceComponent)}");
            return false;
        }

        experienceEntity = (experienceUid, experienceComponent);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryChangeStudyingProgress(EntityUid skillUid, ProtoId<SkillTreePrototype> skillTree, LearningInformation info)
    {
        if (!ResolveExperienceEntityFromSkillEntity(skillUid, out var experienceEntity))
            return false;

        return Experience.TryChangeStudyingProgress(experienceEntity.Value!, skillTree, info);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryChangeStudyingProgress(EntityUid skillUid, ProtoId<SkillTreePrototype> skillTree, FixedPoint4 delta)
    {
        if (!ResolveExperienceEntityFromSkillEntity(skillUid, out var experienceEntity))
            return false;

        return Experience.TryChangeStudyingProgress(experienceEntity.Value!, skillTree, delta);
    }

    /// <summary>
    /// Resolves owner entity of skill and writes log with adding parent entity
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAddToAdminLogs<T>(Entity<T> entity, string message, LogImpact logImpact = LogImpact.Low) where T : IComponent
    {
        if (!ResolveExperienceEntityFromSkillEntity(entity.Owner, out var experienceEntity))
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
        var seed = SharedRandomExtensions.HashCodeCombine(valuesForSeed);
        return new System.Random(seed);
    }

    /// <summary>
    /// Random that gives same result on client and on server
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public System.Random GetPredictedRandomOnCurTick(in List<int> valuesForSeed)
    {
        var toCombine = new List<int>(valuesForSeed);
        toCombine.Add((int)GameTiming.CurTick.Value);

        var seed = SharedRandomExtensions.HashCodeCombine(toCombine);
        return new System.Random(seed);
    }
}
