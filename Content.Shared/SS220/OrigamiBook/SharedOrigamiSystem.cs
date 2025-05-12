using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.OrigamiBook;

public abstract class SharedOrigamiSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<OrigamiUserComponent, BeforeThrowEvent>(OnBeforeThrow);

        SubscribeLocalEvent<OrigamiWeaponComponent, ThrowDoHitEvent>(OnThrowHit);

        SubscribeLocalEvent<StartLearnOrigamiDoAfter>(OnDoAfter);
    }

    private void OnBeforeThrow(Entity<OrigamiUserComponent> ent, ref BeforeThrowEvent args)
    {
        if (!TryComp<OrigamiWeaponComponent>(args.ItemUid, out var origamiWeapon))
            return;

        args.ThrowSpeed *= origamiWeapon.ThrowSpeedIncrease;
    }

    private void OnThrowHit(Entity<OrigamiWeaponComponent> ent, ref ThrowDoHitEvent args)
    {
        if (!HasComp<OrigamiUserComponent>(args.Component.Thrower))
            return;

        if (TryComp<IdentityBlockerComponent>(args.Target, out var identityBlocker)
            && identityBlocker.Coverage is IdentityBlockerCoverage.EYES or IdentityBlockerCoverage.FULL)
        {
            _damageable.TryChangeDamage(args.Target, ent.Comp.Damage);
            return;
        }

        _damageable.TryChangeDamage(args.Target, ent.Comp.DamageWithoutGlasses);
        _stun.TryParalyze(args.Target, TimeSpan.FromSeconds(ent.Comp.TimeParalyze), true);
    }

    private void OnDoAfter(StartLearnOrigamiDoAfter args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!TryComp<OrigamiBookComponent>(args.Book, out var origamiBook))
            return;

        if (_random.Prob(origamiBook.ChanceToLearn))
        {
            EnsureComp<OrigamiUserComponent>(args.User);
            _popup.PopupEntity(Loc.GetString("origami-book-success-learned"), args.User, args.User);
            RemCompDeferred<OrigamiBookComponent>(args.Book);
            return;
        }

        _popup.PopupEntity(Loc.GetString("origami-book-failed-learned"), args.User, args.User);
        args.Handled = true;
    }
}

[Serializable]
[NetSerializable]
public sealed partial class StartLearnOrigamiDoAfter : DoAfterEvent
{
    [NonSerialized]
    public EntityUid Book;

    public StartLearnOrigamiDoAfter(EntityUid book)
    {
        Book = book;
    }

    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class TransformPaperToAirplaneDoAfter : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}
