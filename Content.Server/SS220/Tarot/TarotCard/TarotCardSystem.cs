using Content.Server.Administration.Systems;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Fluids.EntitySystems;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Server.SS220.DieOfFate;
using Content.Server.Stunnable;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Doors.Systems;
using Content.Shared.Examine;
using Content.Shared.Hands;
using Content.Shared.Humanoid;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.NPC.Systems;
using Content.Shared.Roles.Jobs;
using Content.Shared.SS220.Tarot;
using Content.Shared.StatusEffectNew;
using Content.Shared.Throwing;
using Content.Shared.Trigger.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.SS220.Tarot.TarotCard;

public sealed partial class TarotCardSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly SharedDoorSystem _door = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly ScramOnTriggerSystem _scramOnTrigger = default!;
    [Dependency] private readonly RejuvenateSystem _rejuvenate = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly SmokeSystem _smoke = default!;
    [Dependency] private readonly DieOfFateSystem _dieOfFate = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TarotCardComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TarotCardComponent, GotEquippedHandEvent>(OnGotEquipped);
        SubscribeLocalEvent<TarotCardComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<TarotCardComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<TarotCardComponent, ThrowDoHitEvent>(OnThrow);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TarotCardComponent>();

        while (query.MoveNext(out var card, out var cardComponent))
        {
            if (cardComponent.EntityEffect != null && !Exists(cardComponent.EntityEffect.Value))
                QueueDel(card);
        }
    }

    private void OnMapInit(Entity<TarotCardComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.IsReversed = _random.Next(2) == 0;
        _appearance.SetData(ent.Owner, TarotVisuals.Reversed, ent.Comp.IsReversed);
    }

    private void OnGotEquipped(Entity<TarotCardComponent> ent, ref GotEquippedHandEvent args)
    {
        ent.Comp.CardOwner = args.User;
    }

    private void OnExamined(Entity<TarotCardComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString(ent.Comp.IsReversed ? "tarot-card-is-reverse" : "tarot-card-is-not-reverse"));
    }

    private void OnUseInHand(Entity<TarotCardComponent> ent, ref UseInHandEvent args)
    {
        if (ent.Comp.IsUsed)
        {
            _popup.PopupEntity(Loc.GetString("tarot-cards-failed-already-used"), args.User, args.User);
            return;
        }

        if (!HasComp<HumanoidAppearanceComponent>(args.User))
            return;

        HandleCardEffect(ent, args.User);
        args.Handled = true;
    }

    private void OnThrow(Entity<TarotCardComponent> ent, ref ThrowDoHitEvent args)
    {
        if (ent.Comp.IsUsed)
            return;

        if (!HasComp<HumanoidAppearanceComponent>(args.Target))
            return;

        HandleCardEffect(ent, args.Target);
        args.Handled = true;
    }

    private void HandleCardEffect(Entity<TarotCardComponent> card, EntityUid target)
    {
        var entityEffect = SpawnAttachedTo(TarotCardEffectPrototype, Transform(target).Coordinates);
        card.Comp.EntityEffect = entityEffect;

        if (!_transform.IsParentOf(Transform(target), entityEffect))
            _transform.SetParent(entityEffect, target);

        switch (card.Comp.CardType)
        {
            case TarotCardType.Fool:
                ApplyReversedEffect(card, target, ClearInventory, TeleportToArrivals);
                break;
            case TarotCardType.Magician:
                ApplyReversedEffect(card, target, PushPlayers, OpenNearestAirlock);
                break;
            case TarotCardType.HighPriestess:
                ApplyReversedEffect(card, target, SpawnRandomAnomaly, Slowdown);
                break;
            case TarotCardType.Empress:
                ApplyReversedEffect(card, target, EnsurePacified, TransferSolution);
                break;
            case TarotCardType.Emperor:
                ApplyReversedEffect(card, target, TeleportToHoD, TeleportToBridge);
                break;
            case TarotCardType.Hierophant:
                ApplyReversedEffect(card, target, SpawnCerberus, SpawnCatCake);
                break;
            case TarotCardType.Lovers:
                ApplyReversedEffect(card, target, HurtTarget, HealTarget);
                break;
            case TarotCardType.Chariot:
                ApplyReversedEffect(card, target, SpeedUpEntity, ApplyChariotEffects);
                break;
            case TarotCardType.Strength:
                ApplyReversedEffect(card, target, MassHallucinations, HealTarget);
                break;
            case TarotCardType.Hermit:
                ApplyReversedEffect(card, target, TransformGuns, TeleportToVend);
                break;
            case TarotCardType.WheelOfFortune:
                ApplyReversedEffect(card, target, RollDieOfFortune, CreateGambling);
                break;
            case TarotCardType.Justice:
                ApplyReversedEffect(card, target, SpawnRandomCrate, SpawnJusticeItems);
                break;
            case TarotCardType.HangedMan:
                ApplyReversedEffect(card, target, FixturesSet, TeleportToRandomTile);
                break;
            case TarotCardType.Death:
                ApplyReversedEffect(card, target, ModifyThreshold, HurtAnother);
                break;
            case TarotCardType.Temperance:
                ApplyReversedEffect(card, target, EatPills, HealLing);
                break;
            case TarotCardType.Devil:
                ApplyReversedEffect(card, target, ClusterFlashBang, HelpMessage);
                break;
            case TarotCardType.Tower:
                break;
            case TarotCardType.Star:
                ApplyReversedEffect(card, target, TeleportToUser, GivePinpointer);
                break;
            case TarotCardType.Moon:
                ApplyReversedEffect(card, target, RandomTeleportation, RandomTeleportation);
                break;
            case TarotCardType.Sun:
                ApplyReversedEffect(card, target, SmokeGrenade, Rejuvenate);
                break;
            case TarotCardType.Judgement:
                break;
            case TarotCardType.World:
                ApplyReversedEffect(card, target, BlockActionInRange, BlockAction);
                break;
        }

        card.Comp.IsUsed = true;
    }

    private static void ApplyReversedEffect(
        TarotCardComponent card,
        EntityUid target,
        Action<EntityUid, EntityUid?> reversedAction,
        Action<EntityUid, EntityUid?> normalAction)
    {
        if (card.IsReversed)
        {
            reversedAction(target, card.CardOwner);
        }
        else
        {
            normalAction(target, card.CardOwner);
        }
    }
}
