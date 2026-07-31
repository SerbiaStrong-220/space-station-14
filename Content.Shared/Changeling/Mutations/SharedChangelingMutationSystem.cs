// SS220 Changeling
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Movement.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Changeling.Mutations;

/// <summary>
/// Prediction-safe portions of changeling mutations. Resource spending and all
/// state transitions are handled by the server system; this system only applies
/// modifiers represented by networked state.
/// </summary>
public sealed class SharedChangelingMutationSystem : EntitySystem
{
    private static readonly ProtoId<DamageTypePrototype> BluntDamage = "Blunt";
    private static readonly ProtoId<DamageTypePrototype> PiercingDamage = "Piercing";
    private static readonly ProtoId<DamageTypePrototype> SlashDamage = "Slash";

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingMutationStateComponent, DamageModifyEvent>(OnDamageModify);
        SubscribeLocalEvent<ChangelingMutationStateComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovement);
        SubscribeLocalEvent<ChangelingMutationStateComponent, AfterAutoHandleStateEvent>(OnMutationStateHandled);
    }

    private void OnDamageModify(Entity<ChangelingMutationStateComponent> ent, ref DamageModifyEvent args)
    {
        if (!ent.Comp.ChitinousArmorActive)
            return;

        var modified = args.Damage.Clone();
        // Enumerate the source dictionary while updating the clone. Mutating the dictionary currently being
        // enumerated relies on runtime-specific Dictionary implementation details and can invalidate the iterator.
        foreach (var (type, damage) in args.Damage.DamageDict)
        {
            if (damage <= 0)
                continue;

            var modifier = type == BluntDamage || type == SlashDamage || type == PiercingDamage
                ? 0.45f
                : 0.75f;
            modified.DamageDict[type] = damage * modifier;
        }

        args.Damage = modified;
    }

    private void OnRefreshMovement(Entity<ChangelingMutationStateComponent> ent,
        ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.ChitinousArmorActive)
            args.ModifySpeed(0.75f);

        if (ent.Comp.StrainedMusclesActive)
            args.ModifySpeed(1.6f);

        if (ent.Comp.EpinephrineEndsAt is { } end && end > _timing.CurTime)
            args.ModifySpeed(1.2f);
    }

    private void OnMutationStateHandled(Entity<ChangelingMutationStateComponent> ent,
        ref AfterAutoHandleStateEvent args)
    {
        _movement.RefreshMovementSpeedModifiers(ent.Owner);
    }
}
