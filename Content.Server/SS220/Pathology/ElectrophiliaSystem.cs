// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.SS220.Pathology;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.SS220.Pathology;

public sealed partial class ElectrophiliaSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedStaminaSystem _stamina = default!;
    [Dependency] private SharedPathologySystem _pathology = default!;

    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);
    private TimeSpan _nextUpdate;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ElectrophiliaComponent, DamageModifyEvent>(OnDamageModify);
        SubscribeLocalEvent<ElectrophiliaComponent, PathologySeverityChanged>(OnSeverityChanged);
    }

    private void OnDamageModify(Entity<ElectrophiliaComponent> ent, ref DamageModifyEvent args)
    {
        if (!args.Damage.DamageDict.TryGetValue(ent.Comp.ShockType, out var shock) || shock <= FixedPoint2.Zero)
            return;

        // any shock feeds the addiction and resets the withdrawal timer
        ent.Comp.LastShock = _timing.CurTime;

        if (!_pathology.TryGetSymptomData(ent.Owner, ent.Comp.Pathology, out var data))
            return;

        // scale the shock damage for the current stage (0.5 -> 0 -> 0)
        var coefCount = ent.Comp.ShockCoefficientPerStage.Count;
        var coefficient = coefCount > 0
            ? ent.Comp.ShockCoefficientPerStage[Math.Min(data.Level, coefCount - 1)]
            : 1f;
        var modifier = new DamageModifierSet();
        modifier.Coefficients[ent.Comp.ShockType] = coefficient;
        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, modifier);

        // final stage - shock comes back as healing instead, across each configured group
        var healCount = ent.Comp.HealFractionPerStage.Count;
        var healFraction = healCount > 0
            ? ent.Comp.HealFractionPerStage[Math.Min(data.Level, healCount - 1)]
            : FixedPoint2.Zero;
        if (healFraction <= FixedPoint2.Zero)
            return;

        var heal = shock * healFraction;
        foreach (var groupId in ent.Comp.HealGroups)
        {
            if (_prototype.TryIndex(groupId, out var group))
                args.Damage += new DamageSpecifier(group, -heal);
        }
    }

    private void OnSeverityChanged(Entity<ElectrophiliaComponent> ent, ref PathologySeverityChanged args)
    {
        if (args.PathologyId == ent.Comp.Pathology)
            ent.Comp.LastShock = _timing.CurTime;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + UpdateInterval;

        var query = EntityQueryEnumerator<ElectrophiliaComponent, PathologyHolderComponent>();
        while (query.MoveNext(out var uid, out var comp, out var holder))
        {
            if (!holder.ActivePathologies.TryGetValue(comp.Pathology, out var data)
                || data.Level < comp.WithdrawalFromStage)
                continue;

            if (_timing.CurTime - comp.LastShock < comp.WithdrawalDelay)
                continue;

            _stamina.TakeStaminaDamage(uid, comp.WithdrawalStaminaDamage);
        }
    }
}
