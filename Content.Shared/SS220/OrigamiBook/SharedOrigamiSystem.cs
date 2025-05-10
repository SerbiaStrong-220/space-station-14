using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.OrigamiBook;

public sealed class SharedOrigamiSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<OrigamiBookComponent, UseInHandEvent>(OnUseInHand);

        SubscribeLocalEvent<OrigamiUserComponent, BeforeThrowEvent>(OnBeforeThrow);

        SubscribeLocalEvent<PaperComponent, GetVerbsEvent<AlternativeVerb>>(OnVerb);
        SubscribeLocalEvent<PaperComponent, TransformPaperToAirplaneDoAfter>(OnTransformPaper);

        SubscribeLocalEvent<OrigamiWeaponComponent, ThrowDoHitEvent>(OnThrowHit);

        SubscribeLocalEvent<HumanoidAppearanceComponent, StartLearnOrigamiDoAfter>(OnDoAfter);
    }

    private void OnUseInHand(Entity<OrigamiBookComponent> ent, ref UseInHandEvent args)
    {
        if (_net.IsClient)
            return;

        if (args.Handled)
            return;

        if (HasComp<OrigamiUserComponent>(args.User))
        {
            _popup.PopupEntity(Loc.GetString("origami-book-already-known"), args.User, args.User);
            args.Handled = true;
            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.DelayToLearn,
            new StartLearnOrigamiDoAfter(ent.Owner),
            args.User)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
            BlockDuplicate = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }

    private void OnBeforeThrow(Entity<OrigamiUserComponent> ent, ref BeforeThrowEvent args)
    {
        if (!TryComp<OrigamiWeaponComponent>(args.ItemUid, out var origamiWeapon))
            return;

        args.ThrowSpeed *= origamiWeapon.ThrowSpeedIncrease;
    }

    private void OnVerb(Entity<PaperComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!TryComp<OrigamiUserComponent>(args.User, out var origamiUserComponent)
            || HasComp<OrigamiWeaponComponent>(ent.Owner))
            return;

        var user = args.User;

        var altVerb = new AlternativeVerb
        {
            Text = Loc.GetString("origami-transform-from-paper"),
            Act = () =>
            {
                var doAfterArgs = new DoAfterArgs(EntityManager,
                    user,
                    origamiUserComponent.DelayToTransform,
                    new TransformPaperToAirplaneDoAfter(),
                    ent.Owner)
                {
                    BlockDuplicate = true,
                };

                _doAfter.TryStartDoAfter(doAfterArgs);
            },
            Priority = 0,
        };

        args.Verbs.Add(altVerb);
    }

    private void OnTransformPaper(Entity<PaperComponent> ent, ref TransformPaperToAirplaneDoAfter args)
    {
        if (_net.IsClient || args.Cancelled)
            return;

        var airPlane = Spawn("PaperAirplane", Transform(ent.Owner).Coordinates);

        QueueDel(ent.Owner);

        _hands.TryPickupAnyHand(args.User, airPlane);
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

    private void OnDoAfter(Entity<HumanoidAppearanceComponent> ent, ref StartLearnOrigamiDoAfter args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!TryComp<OrigamiBookComponent>(args.Book, out var origamiBook))
            return;

        if (_random.Prob(origamiBook.ChanceToLearn))
        {
            EnsureComp<OrigamiUserComponent>(ent.Owner);
            _popup.PopupEntity(Loc.GetString("origami-book-success-learned"), ent.Owner, ent.Owner);
            RemCompDeferred<OrigamiBookComponent>(args.Book);
            return;
        }

        _popup.PopupEntity(Loc.GetString("origami-book-failed-learned"), ent.Owner, ent.Owner);
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
