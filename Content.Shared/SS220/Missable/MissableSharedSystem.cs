// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Random;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Content.Shared.SS220.Damage;

namespace Content.Shared.SS220.Missable;

public sealed class MissableSharedSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly INetManager _net = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MissableComponent, AttackedEvent>(RollChanceToMiss);
        SubscribeLocalEvent<GetDamageOtherOnHitEvent>(ThrowChanceMiss);
    }

    private void ThrowChanceMiss(GetDamageOtherOnHitEvent args)
    {
        if (HasChance(GetEntity(args.Target)))
            args.Handled = true;
    }
    private bool HasChance(EntityUid entity)
    {
        if (!HasComp<MissableComponent>(entity))
            return false;

        var missEvent = new MissableCanMissEvent();
        RaiseLocalEvent(entity, missEvent);

        bool anywayMiss = false;
        if (missEvent.Handled && missEvent.ShouldBeMiss)
            anywayMiss = true;

        var bonusEvent = new MissableMissChanceBonusEvent();
        RaiseLocalEvent(entity, bonusEvent);

        float chanceBonus = 0f;
        foreach (var item in bonusEvent.BonusMiss)
        {
            if (item > chanceBonus)
                chanceBonus = item;
        }
        var hitChance = _random.NextFloat(0f, 1f);
        if (anywayMiss || chanceBonus >= hitChance)
        {
            if (_net.IsServer)
                _popup.PopupEntity(Loc.GetString("missable-miss"), entity);
            return true;
        }
        return false;
    }
    private void RollChanceToMiss(EntityUid entity, MissableComponent comp, AttackedEvent args)
    {
        args.Cancelled = HasChance(entity);
    }
}