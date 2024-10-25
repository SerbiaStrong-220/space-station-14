// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Server.GameObjects;
using Content.Shared.SS220.CultYogg.MiGo;
using Content.Shared.Audio;
using Content.Server.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server.SS220.CultYogg.Nyarlathotep;

/// <summary>
/// Searches for entities within a given radius to further pursue them
/// </summary>
public sealed class NyarlathotepTargetSearcherSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly ServerGlobalSoundSystem _sound = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<NyarlathotepSearchTargetsComponent, MapInitEvent>(OnSearchMapInit);

        SubscribeLocalEvent<NyarlathotepSearchTargetsComponent, ComponentStartup>(OnCompInit);
    }

    private void OnCompInit(Entity<NyarlathotepSearchTargetsComponent> uid, ref ComponentStartup args)
    {
        var selectedSong = _audio.GetSound(uid.Comp.SummonMusic);
        if (!string.IsNullOrEmpty(selectedSong))
            _sound.DispatchStationEventMusic(uid, selectedSong, StationEventMusicType.Nuke);//ToDo should i rename?
        var ev = new CultYoggSummonedEvent(uid);
        RaiseLocalEvent(uid, ref ev, true);
    }

    /// <summary>
    /// Adds a component to pursue targets
    /// Performs a duplicate component check, on the MiGi component to not harass cult members
    /// and cuts off entities that are not alive
    /// </summary>
    public void SearchNearNyarlathotep(EntityUid user, float range)
    {
        foreach (var target in _entityLookupSystem.GetEntitiesInRange<MobStateComponent>(_transform.GetMapCoordinates(user), range))
        {
            if (HasComp<MiGoComponent>(target.Owner))
                continue;

            if (HasComp<NyarlathotepTargetComponent>(target.Owner))
                continue;

            if (_mobStateSystem.IsAlive(target.Owner))
                AddComp(target.Owner, new NyarlathotepTargetComponent());
        }
    }
    private void OnSearchMapInit(Entity<NyarlathotepSearchTargetsComponent> component, ref MapInitEvent args)
    {
        component.Comp.NextSearchTime = _gameTiming.CurTime + TimeSpan.FromSeconds(component.Comp.SearchMaxInterval);
    }

    /// <summary>
    /// Updates the target seeker's cooldowns.
    /// Periodically checks for new targets in the radius.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<NyarlathotepSearchTargetsComponent>();
        while (query.MoveNext(out var uid, out var targetSearcher))
        {
            if (targetSearcher.NextSearchTime > _gameTiming.CurTime)
                continue;

            TargetSearch(uid, targetSearcher);
            var delay = TimeSpan.FromSeconds(_random.NextFloat(targetSearcher.SearchMinInterval, targetSearcher.SearchMaxInterval));
            targetSearcher.NextSearchTime += delay;
        }
    }

    private void TargetSearch(EntityUid uid, NyarlathotepSearchTargetsComponent component)
    {
        SearchNearNyarlathotep(uid, component.SearchRange);
    }
}

/// <summary>
///     Raised when god summoned to markup winning
/// </summary>
[ByRefEvent, Serializable]
public sealed class CultYoggSummonedEvent : EntityEventArgs
{
    public readonly EntityUid Entity;

    public CultYoggSummonedEvent(EntityUid entity)
    {
        Entity = entity;
    }
}
/// <summary>
/// Component for entities to be attacked by Nyarlathotep.
/// </summary>
[RegisterComponent]
public sealed partial class NyarlathotepTargetComponent : Component;
