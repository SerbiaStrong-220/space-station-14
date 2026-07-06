// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.SS220.Virology.Behaviors;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.SS220.Virology.Behaviors;

public sealed partial class VirusElectrophiliaSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedStaminaSystem _stamina = default!;

    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);
    private TimeSpan _nextUpdate;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VirusElectrophiliaComponent, DamageModifyEvent>(OnDamageModify);
        SubscribeLocalEvent<VirusElectrophiliaComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<VirusElectrophiliaComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.LastShock = _timing.CurTime;
    }

    private void OnDamageModify(Entity<VirusElectrophiliaComponent> ent, ref DamageModifyEvent args)
    {
        if (!args.Damage.DamageDict.TryGetValue(ent.Comp.ShockType, out var shock) || shock <= FixedPoint2.Zero)
            return;

        // any shock feeds withdrawal timer
        ent.Comp.LastShock = _timing.CurTime;

        var modifier = new DamageModifierSet();
        modifier.Coefficients[ent.Comp.ShockType] = ent.Comp.ShockCoefficient;
        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, modifier);

        if (ent.Comp.HealFraction <= FixedPoint2.Zero || ent.Comp.HealTypes.Count == 0)
            return;

        var heal = shock * ent.Comp.HealFraction / ent.Comp.HealTypes.Count;
        foreach (var typeId in ent.Comp.HealTypes)
        {
            if (_prototype.TryIndex(typeId, out var type))
                args.Damage += new DamageSpecifier(type, -heal);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + UpdateInterval;

        var query = EntityQueryEnumerator<VirusElectrophiliaComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.Withdrawal)
                continue;

            if (_timing.CurTime - comp.LastShock < comp.WithdrawalDelay)
                continue;

            _stamina.TakeStaminaDamage(uid, comp.WithdrawalStaminaDamage);
        }
    }
}
