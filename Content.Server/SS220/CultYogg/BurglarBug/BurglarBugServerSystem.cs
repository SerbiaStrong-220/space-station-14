// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Administration.Logs;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Popups;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Doors.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Sticky;
using Content.Shared.Sticky.Components;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Server.SS220.CultYogg.BurglarBug;

public sealed class BurglarBugServerSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BurglarBugComponent, TriggerEvent>(HandleTrigger);
        SubscribeLocalEvent<BurglarBugComponent, UseInHandEvent>(OnActivate);
        SubscribeLocalEvent<BurglarBugComponent, AttemptEntityStickEvent>(OnStick);
        SubscribeLocalEvent<BurglarBugComponent, StickyDoAfterEvent>(OnBreak);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<BurglarBugComponent>();
        while (query.MoveNext(out var uid, out var bugComponent))
        {
            if (bugComponent.DoorOpenTime == null || bugComponent.DoorOpenTime > _gameTiming.CurTime)
                continue;

            OpenDoor(uid, bugComponent);
        }
    }
    private void OpenDoor(EntityUid uid, BurglarBugComponent component)
    {
            if (!HasComp<StickyComponent>(uid))
                return;

            if (TryComp<DoorComponent>(component.Door, out var door))
            {
                door.ClickOpen = true;
                door.BumpOpen = true;
                Dirty(component.Door.Value, door);
                var emaggedEvent = new GotEmaggedEvent();
                RaiseLocalEvent(component.Door.Value, ref emaggedEvent);
            }

            AfterUsed((uid, component));
        }
    private void OnBreak(Entity<BurglarBugComponent> entity, ref StickyDoAfterEvent args)
    {
        if (args.Cancelled)
        {
            if (TryComp<DoorComponent>(entity.Comp.Door, out var dor))
            {
                dor.ClickOpen = true;
                dor.BumpOpen = true;
                Dirty(entity.Comp.Door.Value, dor);
            }
            entity.Comp.Activated = false;
            return;
        }

        entity.Comp.DoorOpenTime = _gameTiming.CurTime + TimeSpan.FromSeconds(entity.Comp.TimeToOpen);
    }
    private void OnActivate(Entity<BurglarBugComponent> entity, ref UseInHandEvent args)
    {
        entity.Comp.Activated = true;
    }

    private void OnStick(Entity<BurglarBugComponent> entity, ref AttemptEntityStickEvent args)
    {
        if (entity.Comp.OpenedDoorStickPopupCancellation != null)
        {
            if (TryComp<DoorComponent>(args.Target,
                    out var doorComponent) &&  doorComponent.State != DoorState.Closed)
            {
                args.Cancelled = true;
                RaiseLocalEvent(entity.Owner, new DroppedEvent(args.User), true);
                var msg = Loc.GetString(entity.Comp.OpenedDoorStickPopupCancellation);
                _popupSystem.PopupEntity(msg, args.User, PopupType.MediumCaution);
                _handsSystem.TryDrop(args.User);
                _adminLogger.Add(LogType.Stripping, LogImpact.Medium, $"{ToPrettyString(args.User):actor} has droped the item {ToPrettyString(entity.Owner):item}");
                return;
            }

        }
        if (!entity.Comp.Activated)
        {
            if (entity.Comp.NotActivatedStickPopupCancellation != null)
            {
                var msg = Loc.GetString(entity.Comp.NotActivatedStickPopupCancellation);
                _popupSystem.PopupEntity(msg, args.User, PopupType.MediumCaution);
            }
            args.Cancelled = true;
            return;
        }
        if (TryComp<DoorComponent>(args.Target, out var door))
        {
            entity.Comp.Door = (args.Target, door);
            door.ClickOpen = false;
            door.BumpOpen = false;
            Dirty(args.Target, door);
        }
    }

    private void HandleTrigger(Entity<BurglarBugComponent> entity, ref TriggerEvent args)
    {
        var (uid, component) = entity;

        if (!TryComp<StickyComponent>(uid, out var stickyComponent))
            return;

        if (stickyComponent.StuckTo == null)
        {
            var damage = new DamageSpecifier(component.Damage);
            var targets = _entityLookupSystem.GetEntitiesInRange(uid, component.DamageRange);
            targets.RemoveWhere(s => !HasComp<MobStateComponent>(s));
            foreach (var target
                     in targets)
            {
                _damageable.TryChangeDamage(target, damage, component.IgnoreResistances);
            }

            AfterUsed(entity);
        }
    }

    private void AfterUsed(Entity<BurglarBugComponent> entity)
    {
        Del(entity);
    }
}
