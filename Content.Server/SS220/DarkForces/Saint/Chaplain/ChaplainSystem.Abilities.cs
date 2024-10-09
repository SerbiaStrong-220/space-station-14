using Content.Server.SS220.DarkForces.Saint.Chaplain.Abilities;
using Content.Server.SS220.DarkForces.Saint.Chaplain.Components;
using Content.Server.Popups;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Mobs.Components;
using Content.Shared.SS220.DarkForces.Saint.Chaplain.Events;
using Content.Shared.SS220.DarkForces.Saint.Chaplain.Events.Narsi;
using Robust.Server.Audio;
using Robust.Shared.Player;
using Content.Server.SS220.Utils;

namespace Content.Server.SS220.DarkForces.Saint.Chaplain;

public sealed partial class ChaplainSystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly AudioSystem _audioSystem = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly StationUtils _stationUtils = default!;

    private static readonly TimeSpan NarsiExileDelay = TimeSpan.FromSeconds(240);
    private static readonly TimeSpan GreatPrayerDelay = TimeSpan.FromSeconds(80);
    private static readonly TimeSpan ExorcismDelay = TimeSpan.FromSeconds(30);

    private const int DefaultChaplainBarrier = 2;
    private const int NarsiExileChaplainBarrier = 4;

    private void InitializeAbilities()
    {
        SubscribeLocalEvent<ChaplainComponent, ChaplainNarsiExileEvent>(OnChaplainNarsiExileRitual);
        SubscribeLocalEvent<ChaplainComponent, ChaplainNarsiExileDoAfterEvent>(OnNarsiChaplainDoAfter);

        SubscribeLocalEvent<ChaplainComponent, ChaplainGreatPrayerEvent>(OnGreatPrayerEvent);
        SubscribeLocalEvent<ChaplainComponent, ChaplainGreatPrayerDoAfterEvent>(OnGreatPrayerDoAfterEvent);

        SubscribeLocalEvent<ChaplainComponent, ChaplainDefenceBarrierEvent>(OnDefenceBarrierEvent);

        SubscribeLocalEvent<ChaplainComponent, ChaplainStartExorcismEvent>(OnStartExorcismEvent);
        SubscribeLocalEvent<ChaplainComponent, ChaplainExorcismDoAfterEvent>(OnExorcismDoAfter);
    }

    private async void OnGreatPrayerDoAfterEvent(EntityUid uid, ChaplainComponent component,
        ChaplainGreatPrayerDoAfterEvent args)
    {
        component.GreatPrayerSoundEntity = _audioSystem.Stop(component.GreatPrayerSoundEntity);

        if (args.Handled || args.Cancelled)
            return;

        var chaplainPos = Transform(uid).Coordinates;
        var query = _entityLookup.GetEntitiesInRange<MobStateComponent>(chaplainPos, 5f);
        foreach (var entity in query)
        {
            _damageable.TryChangeDamage(entity, component.FelHealDamage);
        }

        args.Handled = true;
    }

    private async void OnDefenceBarrierEvent(EntityUid uid, ChaplainComponent component,
        ChaplainDefenceBarrierEvent args)
    {
        if (args.Handled)
            return;

        await SpawnBarriers(uid, DefaultChaplainBarrier, ChaplainForceWallDefault);

        args.Handled = true;
    }

    private async void OnGreatPrayerEvent(EntityUid uid, ChaplainComponent component, ChaplainGreatPrayerEvent args)
    {
        if (args.Handled)
            return;

        component.GreatPrayerSoundEntity = _audioSystem.PlayEntity(
                sound: component.GreatPrayerSound,
                playerFilter: Filter.Pvs(uid, entityManager: EntityManager),
                uid: uid,
                recordReplay: true
            )
            ?.Entity;

        var doAfterArgs = GetGreatPrayerDoAfterArgs(uid);
        _doAfterSystem.TryStartDoAfter(doAfterArgs);

        args.Handled = true;
    }

    private DoAfterArgs GetGreatPrayerDoAfterArgs(EntityUid uid)
    {
        var doAfterEvent = new ChaplainGreatPrayerDoAfterEvent();
        var doAfterEventArgs = new DoAfterArgs(
            entManager: EntityManager,
            user: uid,
            delay: GreatPrayerDelay,
            @event: doAfterEvent,
            eventTarget: uid
        )
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 2.0f
        };

        return doAfterEventArgs;
    }

    private void OnNarsiChaplainDoAfter(EntityUid uid, ChaplainComponent component, ChaplainNarsiExileDoAfterEvent args)
    {
        if (args.Handled)
            return;

        if (args.Cancelled)
        {
            RaiseLocalEvent(new ChaplainNarsiExileCanceledEvent(uid));
            return;
        }

        RaiseLocalEvent(new ChaplainNarsiExileFinishedEvent(uid));
        args.Handled = true;
    }

    private async void OnChaplainNarsiExileRitual(EntityUid uid, ChaplainComponent component,
        ChaplainNarsiExileEvent args)
    {
        if (args.Handled)
            return;

        if (!_stationUtils.IsOnMainStationGrid(args.Performer))
        {
            _popupSystem.PopupEntity(Loc.GetString("chaplain-narsi-exile-at-station"), args.Performer);
            return;
        }

        await SpawnBarriers(args.Performer, NarsiExileChaplainBarrier, ChaplainForceWallNarsi);

        var doAfterEventArgs = GetNarsiExileDoAfterArgs(args.Performer);
        _doAfterSystem.TryStartDoAfter(doAfterEventArgs);
        RaiseLocalEvent(new ChaplainNarsiExileStartEvent(args.Performer));

        args.Handled = true;
    }

    private DoAfterArgs GetNarsiExileDoAfterArgs(EntityUid uid)
    {
        var doAfterEvent = new ChaplainNarsiExileDoAfterEvent();
        var doAfterEventArgs = new DoAfterArgs(
            entManager: EntityManager,
            user: uid,
            delay: NarsiExileDelay,
            @event: doAfterEvent,
            eventTarget: uid
        )
        {
            BreakOnMove = true,
            BreakOnDamage = false,
            MovementThreshold = 6.0f
        };

        return doAfterEventArgs;
    }


    private void OnStartExorcismEvent(EntityUid uid, ChaplainComponent component, ChaplainStartExorcismEvent args)
    {
        if (args.Handled)
            return;

        var doAfterEventArgs = GetExorcismDoAfterArgs(args.Performer, args.Target);
        _doAfterSystem.TryStartDoAfter(doAfterEventArgs);

        component.GreatPrayerSoundEntity = _audioSystem.PlayEntity(
                sound: component.GreatPrayerSound,
                playerFilter: Filter.Pvs(uid, entityManager: EntityManager),
                uid: uid,
                recordReplay: true
            )
            ?.Entity;

        args.Handled = true;
    }

    private DoAfterArgs GetExorcismDoAfterArgs(EntityUid uid, EntityUid target)
    {
        var doAfterEvent = new ChaplainExorcismDoAfterEvent();
        var doAfterEventArgs = new DoAfterArgs(
            entManager: EntityManager,
            user: uid,
            delay: ExorcismDelay,
            @event: doAfterEvent,
            eventTarget: uid,
            target: target
        )
        {
            BreakOnMove = true,
            MovementThreshold = 1.5f
        };

        return doAfterEventArgs;
    }

    private void OnExorcismDoAfter(EntityUid uid, ChaplainComponent component, ChaplainExorcismDoAfterEvent args)
    {
        component.GreatPrayerSoundEntity = _audioSystem.Stop(component.GreatPrayerSoundEntity);

        var target = args.Target;
        if (args.Handled || args.Cancelled || target == null)
            return;

        var ev = new ChaplainExorcismEvent();
        RaiseLocalEvent(target.Value, ev);

        args.Handled = true;
    }
}
