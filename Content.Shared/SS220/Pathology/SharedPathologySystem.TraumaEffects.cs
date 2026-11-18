// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DoAfter;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.SS220.ChangeSpeedDoAfters.Events;
using Content.Shared.SS220.Surgery;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Pathology;

public abstract partial class SharedPathologySystem
{
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedGunSystem _gun = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    private void InitializeTraumaEffectsEvents()
    {
        SubscribeLocalEvent<PathologyHolderComponent, BeforeDoAfterStartEvent>(OnBeforeDoAfterStart);
        SubscribeLocalEvent<PathologyHolderComponent, PathologyRemovedEvent>(OnPathologyRemoved);

        SubscribeLocalEvent<GunComponent, GotEquippedHandEvent>(OnGunEquippedHand);
        SubscribeLocalEvent<GunComponent, GotUnequippedHandEvent>(OnGunUnequippedHand);
        SubscribeLocalEvent<GunComponent, ItemWieldedEvent>(OnGunWielded);
        SubscribeLocalEvent<GunComponent, ItemUnwieldedEvent>(OnGunUnwielded);
        SubscribeLocalEvent<GunComponent, GunRefreshModifiersEvent>(OnGunRefreshModifiers, after: [typeof(BatteryWeaponFireModesSystem)]);
    }

    private void OnBeforeDoAfterStart(Entity<PathologyHolderComponent> ent, ref BeforeDoAfterStartEvent args)
    {
        if (args.Args.ArgFlags.HasFlag(DoAfterArgFlags.IgnoreTraitsModification))
            return;

        var totalModifier = 1f;
        foreach (var (protoId, data) in ent.Comp.ActivePathologies)
        {
            if (!TryResolveDefinition(protoId, data.Level, out _, out var definition))
                continue;

            if (!MathHelper.CloseTo(definition.DoAfterDelayModifier, 1f))
                totalModifier *= definition.DoAfterDelayModifier;
        }

        if (!MathHelper.CloseTo(totalModifier, 1f))
            args.Args.DelayModifier *= totalModifier;
    }

    private void HandleTraumaSeverityChanged(Entity<PathologyHolderComponent> ent, ref PathologySeverityChanged args)
    {
        RefreshHeldGuns(ent.Owner);
    }

    private void OnPathologyRemoved(Entity<PathologyHolderComponent> ent, ref PathologyRemovedEvent args)
    {
        RefreshHeldGuns(ent.Owner);
    }

    private void OnGunEquippedHand(Entity<GunComponent> ent, ref GotEquippedHandEvent args)
    {
        _gun.RefreshModifiers((ent.Owner, ent.Comp));
    }

    private void OnGunUnequippedHand(Entity<GunComponent> ent, ref GotUnequippedHandEvent args)
    {
        _gun.RefreshModifiers((ent.Owner, ent.Comp));
    }

    private void OnGunWielded(Entity<GunComponent> ent, ref ItemWieldedEvent args)
    {
        _gun.RefreshModifiers((ent.Owner, ent.Comp));
    }

    private void OnGunUnwielded(Entity<GunComponent> ent, ref ItemUnwieldedEvent args)
    {
        _gun.RefreshModifiers((ent.Owner, ent.Comp));
    }

    private void OnGunRefreshModifiers(Entity<GunComponent> ent, ref GunRefreshModifiersEvent args)
    {
        if (!_container.TryGetContainingContainer((ent.Owner, null), out var container))
            return;

        if (!TryComp<HandsComponent>(container.Owner, out var hands) ||
            !_hands.TryGetHand((container.Owner, hands), container.ID, out var heldHand))
        {
            return;
        }

        if (!TryComp<PathologyHolderComponent>(container.Owner, out var holder))
            return;

        var isWielded = TryComp<WieldableComponent>(ent.Owner, out var wield) && wield.Wielded;
        var penalty = GetGunSpreadPenalty(holder, heldHand.Value.Location, isWielded);

        if (penalty <= Angle.Zero)
            return;

        args.MinAngle += penalty;
        args.MaxAngle += penalty;

        if (args.MaxAngle < args.MinAngle)
            args.MaxAngle = args.MinAngle;
    }

    private Angle GetGunSpreadPenalty(PathologyHolderComponent holder, HandLocation heldLocation, bool isWielded)
    {
        var maxPenalty = Angle.Zero;

        foreach (var (protoId, data) in holder.ActivePathologies)
        {
            if (!TryResolveDefinition(protoId, data.Level, out var proto, out var definition))
                continue;

            if (definition.GunSpreadPenalty <= Angle.Zero ||
                proto.TargetZone is not { } zone ||
                !TryResolveHandLocation(zone, out var traumaHand))
            {
                continue;
            }

            if (!isWielded && traumaHand != heldLocation)
                continue;

            if (definition.GunSpreadPenalty > maxPenalty)
                maxPenalty = definition.GunSpreadPenalty;
        }

        return maxPenalty;
    }

    protected void RefreshHeldGuns(EntityUid holderUid)
    {
        if (!TryComp<HandsComponent>(holderUid, out var hands))
            return;

        foreach (var held in _hands.EnumerateHeld((holderUid, hands)))
        {
            if (TryComp<GunComponent>(held, out var gun))
                _gun.RefreshModifiers((held, gun));
        }
    }

    private bool TryResolveDefinition(
        ProtoId<PathologyPrototype> protoId,
        int level,
        out PathologyPrototype proto,
        out PathologyDefinition definition)
    {
        definition = default!;
        if (!_prototype.Resolve(protoId, out proto!))
            return false;

        if (level < 0 || level >= proto.Definition.Length)
            return false;

        definition = proto.Definition[level];
        return true;
    }

    private static bool TryResolveHandLocation(SurgeryPuppetPart zone, out HandLocation location)
    {
        switch (zone)
        {
            case SurgeryPuppetPart.LeftHand:
                location = HandLocation.Left;
                return true;
            case SurgeryPuppetPart.RightHand:
                location = HandLocation.Right;
                return true;
            default:
                location = default;
                return false;
        }
    }
}
