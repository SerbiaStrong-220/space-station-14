using System.Linq;
using Content.Server.SS220.DarkForces.Ratvar.Righteous.Abilities.Enchantment;
using Content.Server.SS220.DarkForces.Ratvar.Righteous.Abilities.Slab;
using Content.Server.SS220.DarkForces.Ratvar.Righteous.Structures.Altar;
using Content.Server.Doors.Systems;
using Content.Server.Emp;
using Content.Shared.Damage;
using Content.Shared.Doors.Components;
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Events;
using Content.Shared.SS220.DarkForces.Ratvar.Righteous.Abilities.Weapons;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Stunnable;
using Content.Shared.Timing;
using Content.Shared.Tools.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using RatvarEnchantmentableComponent =
    Content.Shared.SS220.DarkForces.Ratvar.Righteous.Abilities.RatvarEnchantmentableComponent;

namespace Content.Server.SS220.DarkForces.Ratvar.Righteous.Abilities;

public sealed partial class RatvarAbilitiesSystem
{
    [ValidatePrototypeId<EntityPrototype>]
    private const string ActionTeleport = "ActionRatvarSlabTeleport";

    [ValidatePrototypeId<EntityPrototype>]
    private const string ActionHiding = "ActionRatvarSlabHidings";

    [ValidatePrototypeId<EntityPrototype>]
    private const string SecretDoor = "SolidSecretDoor";

    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly DoorSystem _doorBoltSystem = default!;
    [Dependency] private readonly DoorSystem _doorSystem = default!;
    [Dependency] private readonly EmpSystem _empSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RatvarHidingSystem _ratvarHidingSystem = default!;
    [Dependency] private readonly SharedStunSystem _stunSystem = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly WeldableSystem _weldableSystem = default!;

    private void InitializeSlab()
    {
        SubscribeLocalEvent<RatvarSlabComponent, RatvarSlabStun>(OnSlabStun);
        SubscribeLocalEvent<RatvarSlabComponent, RatvarSlabDoors>(OnSlabDoors);
        SubscribeLocalEvent<RatvarSlabComponent, RatvarSlabWalls>(OnSlabWalls);
        SubscribeLocalEvent<RatvarSlabComponent, RatvarSlabHidings>(OnHidings);
        SubscribeLocalEvent<RatvarSlabComponent, RatvarSlabTeleport>(OnTeleport);
        SubscribeLocalEvent<RatvarSlabComponent, RatvarSlabHealing>(OnHealing);
        SubscribeLocalEvent<RatvarSlabComponent, UseInHandEvent>(UseSlabInHand);
        SubscribeLocalEvent<RatvarHidingItemComponent, UseInHandEvent>(UseHidingInHand);
    }

    private void OnHealing(EntityUid uid, RatvarSlabComponent component, RatvarSlabHealing args)
    {
        if (args.Handled)
            return;

        if (!HasComp<HumanoidAppearanceComponent>(args.Performer))
            return;

        _damageable.TryChangeDamage(args.Performer, component.HealingHumanoidDamage);
        args.Handled = true;
    }

    private void UseHidingInHand(EntityUid uid, RatvarHidingItemComponent component, UseInHandEvent args)
    {
        _ratvarHidingSystem.BackItem(args.User, uid);
        args.Handled = true;
    }

    private void UseSlabInHand(EntityUid uid, RatvarSlabComponent component, UseInHandEvent args)
    {
        if (!TryComp<RatvarEnchantmentableComponent>(uid, out var enchantmentable))
            return;

        switch (enchantmentable.ActionId)
        {
            case ActionTeleport:
                TeleportToRandomAltar(args.User, uid);
                args.Handled = true;
                break;
            case ActionHiding:
                HideSlub(args.User, uid);
                args.Handled = true;
                break;
        }
    }

    private void TeleportToRandomAltar(EntityUid user, EntityUid slab)
    {
        if (!TryComp<UseDelayComponent>(slab, out var delay) || _useDelay.IsDelayed((slab, delay)))
            return;

        var userTransform = Transform(user);
        var query = EntityQuery<RatvarAltarComponent, TransformComponent>()
            .Where(altar => altar.Item2.MapID == userTransform.MapID)
            .ToList();

        if (!query.Any())
            return;

        var altar = _random.Pick(query).Item1.Owner;
        var transform = Transform(altar);

        _transformSystem.SetCoordinates(user, transform.Coordinates);
        _transformSystem.AttachToGridOrMap(user);
        _useDelay.TryResetDelay((slab, delay));
    }

    private void OnTeleport(EntityUid uid, RatvarSlabComponent component, RatvarSlabTeleport args)
    {
        if (args.Handled)
            return;

        var transform = Transform(args.Performer);
        if (transform.MapID != args.Target.GetMapId(EntityManager))
            return;

        _transformSystem.SetCoordinates(args.Performer, args.Target);
        _transformSystem.AttachToGridOrMap(args.Performer);

        args.Handled = true;
    }

    private void HideSlub(EntityUid user, EntityUid slab)
    {
        if (!TryComp<UseDelayComponent>(slab, out var delayComponent) || _useDelay.IsDelayed((slab, delayComponent)))
            return;

        _ratvarHidingSystem.HideItem(user, slab);
    }

    private void OnHidings(EntityUid uid, RatvarSlabComponent component, RatvarSlabHidings args)
    {
        if (args.Handled)
            return;

        if (_ratvarHidingSystem.IsTargetHidingStructure(args.Target) && _ratvarHidingSystem.BackStructure(args.Target, out var hidingSlab))
        {
            if (TryComp<RatvarSlabComponent>(hidingSlab, out var slabComponent))
            {
                slabComponent.HidingStructure = false;
            }

            args.Handled = true;
            return;
        }

        if (component.HidingStructure || !_ratvarHidingSystem.HideStructure(args.Target, uid))
            return;

        component.HidingStructure = true;
        args.Handled = true;
    }

    private void OnSlabDoors(EntityUid uid, RatvarSlabComponent component, RatvarSlabDoors args)
    {
        if (args.Handled)
            return;

        var target = args.Target;
        //TODO шкафы
        if (!TryComp<DoorComponent>(target, out var doorComponent))
            return;

        if (doorComponent.State == DoorState.Welded)
        {
            _weldableSystem.SetWeldedState(target, false);
            args.Handled = true;
            return;
        }

        if (TryComp<DoorBoltComponent>(target, out var doorBolt) && _doorBoltSystem.IsBolted(target))
        {
            _doorBoltSystem.SetBoltsDown((target, doorBolt), false);
            args.Handled = true;
            return;
        }

        _doorSystem.TryOpen(target);
        args.Handled = true;
    }

    private void OnSlabStun(EntityUid uid, RatvarSlabComponent component, RatvarSlabStun args)
    {
        if (args.Handled)
            return;

        var target = args.Target;
        if (HasComp<BorgChassisComponent>(target))
        {
            _empSystem.TryEmpEffects(target, 30000, 15);
            args.Handled = true;
            return;
        }

        _stunSystem.TryParalyze(target, TimeSpan.FromSeconds(8), true);
        args.Handled = true;
    }

    private void OnSlabWalls(EntityUid uid, RatvarSlabComponent component, RatvarSlabWalls args)
    {
        var target = args.Target;
        var transform = Transform(target);
        Spawn(SecretDoor, transform.Coordinates);
        Spawn(WallConvertEffect, transform.Coordinates);

        QueueDel(target);
    }
}
