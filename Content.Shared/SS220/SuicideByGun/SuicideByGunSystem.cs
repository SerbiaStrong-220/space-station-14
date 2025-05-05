// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Popups;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.Player;

namespace Content.Shared.SS220.SuicideByGun;

public sealed partial class SuicideByGunSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SuicideByGunComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<SuicideDoAfterEvent>(OnDoAfterComplete);
    }

    private void OnGetVerbs(EntityUid uid, SuicideByGunComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;
        if (!_hands.IsHolding(user, uid, out _))
            return;

        Verb verb = new()
        {
            Act = () => StartSuicide(user, uid),
            Text = Loc.GetString("suicide-verb-name"),
            Priority = 1
        };

        args.Verbs.Add(verb);
    }

    private void StartSuicide(EntityUid user, EntityUid weapon)
    {
        var args = new DoAfterArgs(EntityManager, user, 5f, new SuicideDoAfterEvent(), weapon, target: user, used: weapon)
        {
            BreakOnMove = true,
            BreakOnHandChange = true,
            BreakOnDamage = true,
            NeedHand = true,
            Broadcast = true
        };

        if (_doAfter.TryStartDoAfter(args))
            _popup.PopupEntity(Loc.GetString("suicide-start-popup"), user, weapon);
    }

    private void OnDoAfterComplete(SuicideDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Used == null)
            return;

        var user = args.User;
        var weapon = args.Used.Value;

        if (!_hands.IsHolding(user, weapon, out _))
        {
            _popup.PopupEntity(Loc.GetString("suicide-failed-popup"), user, user);
            return;
        }

        var damage = new DamageSpecifier();
        damage.DamageDict.Add("Blunt", 200);
        _damageable.TryChangeDamage(user, damage, true);

        _popup.PopupEntity(Loc.GetString("suicide-success-popup"), user, user);
        args.Handled = true;
    }
}
