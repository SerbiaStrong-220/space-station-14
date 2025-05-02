// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Buckle.Components;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.SS220.CultYogg.Altar;
using Content.Shared.SS220.CultYogg.Sacraficials;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using System.Linq;
using Content.Shared.Item;
using Content.Shared.Hands;
using Content.Shared.SS220.CultYogg.Buildings;
using Robust.Shared.Prototypes;
using Content.Shared.Mindshield.Components;
using Content.Shared.Zombies;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Verbs;
using Robust.Shared.Utility;
using Content.Shared.Mobs.Components;
using Robust.Shared.Audio;

namespace Content.Shared.SS220.CultYogg.MiGo;

public abstract class SharedMiGoSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedMiGoErectSystem _miGoErectSystem = default!;
    [Dependency] private readonly SharedMiGoPlantSystem _miGoPlantSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedCultYoggHealSystem _heal = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MiGoComponent, ComponentStartup>(OnCompInit);

        // actions
        SubscribeLocalEvent<MiGoComponent, MiGoHealEvent>(MiGoHeal);
        SubscribeLocalEvent<MiGoComponent, MiGoErectEvent>(MiGoErect);
        SubscribeLocalEvent<MiGoComponent, MiGoSacrificeEvent>(MiGoSacrifice);
        SubscribeLocalEvent<MiGoComponent, MiGoAstralEvent>(MiGoAstral);

        //astral DoAfterEvents
        SubscribeLocalEvent<MiGoComponent, AfterMaterialize>(OnAfterMaterialize);
        SubscribeLocalEvent<MiGoComponent, AfterDeMaterialize>(OnAfterDeMaterialize);

        SubscribeLocalEvent<MiGoComponent, AttackAttemptEvent>(CheckAct);
        SubscribeLocalEvent<MiGoComponent, DropAttemptEvent>(OnDropAttempt);
        SubscribeLocalEvent<MiGoComponent, ThrowAttemptEvent>(OnThrowAttempt);
        SubscribeLocalEvent<MiGoComponent, GettingUsedAttemptEvent>(OnBeingUsedAttempt);
        SubscribeLocalEvent<MiGoComponent, GettingPickedUpAttemptEvent>(OnGettingPickedUpAttempt);

        SubscribeLocalEvent<MiGoComponent, BoundUIOpenedEvent>(OnBoundUIOpened);

        SubscribeLocalEvent<MiGoComponent, MiGoEnslavementActionEvent>(OnMiGoEnslaveAction);

        SubscribeLocalEvent<GetVerbsEvent<Verb>>(OnGetVerb);
    }

    protected virtual void OnCompInit(Entity<MiGoComponent> uid, ref ComponentStartup args)
    {
        _actions.AddAction(uid, ref uid.Comp.MiGoHealActionEntity, uid.Comp.MiGoHealAction);
        _actions.AddAction(uid, ref uid.Comp.MiGoEnslavementActionEntity, uid.Comp.MiGoEnslavementAction);
        _actions.AddAction(uid, ref uid.Comp.MiGoAstralActionEntity, uid.Comp.MiGoAstralAction);
        _actions.AddAction(uid, ref uid.Comp.MiGoErectActionEntity, uid.Comp.MiGoErectAction);
        _actions.AddAction(uid, ref uid.Comp.MiGoSacrificeActionEntity, uid.Comp.MiGoSacrificeAction);
    }

    private void OnBoundUIOpened(Entity<MiGoComponent> entity, ref BoundUIOpenedEvent args)
    {
        if (args.UiKey.ToString() == "Erect")
        {
            _userInterfaceSystem.SetUiState(args.Entity, args.UiKey, new MiGoErectBuiState()
            {
                Buildings = _proto.GetInstances<CultYoggBuildingPrototype>().Values.ToList(),
            });
            return;
        }

        if (args.UiKey.ToString() == "Plant")
        {
            _userInterfaceSystem.SetUiState(args.Entity, args.UiKey, new MiGoPlantBuiState()
            {
                Seeds = _proto.GetInstances<CultYoggSeedsPrototype>().Values.ToList(),
            });
            return;
        }
    }

    private void OnGetVerb(GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess ||
            args.User == args.Target)
            return;

        // Enslave verb
        if (TryComp<MiGoComponent>(args.User, out var miGoComp) && miGoComp.IsPhysicalForm)
        {
            var enslaveVerb = new Verb
            {
                Text = Loc.GetString("cult-yogg-enslave-verb"),
                Icon = new SpriteSpecifier.Rsi(new ResPath("SS220/Interface/Actions/cult_yogg.rsi"), "enslavement"),
                Act = () =>
                {
                    if (!CanEnslaveTarget((args.User, miGoComp), args.Target, out var reason))
                    {
                        _popup.PopupPredicted(reason, args.Target, args.User);
                        return;
                    }

                    StartEnslaveDoAfter((args.User, miGoComp), args.Target);
                }
            };

            //ToDo for a future verb
            /*
            var healVerb = new Verb
            {
                Text = Loc.GetString("cult-yogg-heal-verb"),
                Icon = new SpriteSpecifier.Rsi(new ResPath("SS220/Interface/Actions/cult_yogg.rsi"), "heal"),
                Act = () =>
                {

                    //MiGoHeal((args.User, miGoComp), args.Target);
                }
            };

            args.Verbs.Add(enslaveVerb);
            args.Verbs.Add(healVerb);
            */
        }
    }

    #region Heal
    private void MiGoHeal(Entity<MiGoComponent> uid, ref MiGoHealEvent args)
    {
        if (args.Handled)
            return;

        if (!uid.Comp.IsPhysicalForm)
            return;

        if (!HasComp<MobStateComponent>(args.Target))
        {
            _popup.PopupClient(Loc.GetString("cult-yogg-cant-heal-this", ("target", args.Target)), args.Target, uid);
            return;
        }

        //check if effect is already applyed
        if (_statusEffectsSystem.HasStatusEffect(args.Target, uid.Comp.RequiedEffect))
        {
            _popup.PopupClient(Loc.GetString("cult-yogg-heal-already-have-effect"), args.Target, uid);
            return;
        }

        _heal.ApplyMiGoHeal(args.Target, uid.Comp.HealingEffectTime);

        var healComponent = EnsureComp<CultYoggHealComponent>(args.Target);
        healComponent.Heal = args.Heal;
        healComponent.BloodlossModifier = args.BloodlossModifier;
        healComponent.ModifyBloodLevel = args.ModifyBloodLevel;
        healComponent.TimeBetweenIncidents = args.TimeBetweenIncidents;
        healComponent.Sprite = args.EffectSprite;
        Dirty(args.Target, healComponent);

        args.Handled = true;
    }
    #endregion

    #region Erect
    private void MiGoErect(Entity<MiGoComponent> entity, ref MiGoErectEvent args)
    {
        //will wait when sw will update ui parts to copy paste, cause rn it has an errors
        if (args.Handled || !TryComp<ActorComponent>(entity, out var actor))
            return;

        if (!entity.Comp.IsPhysicalForm)
            return;

        _miGoErectSystem.OpenUI(entity, actor);
    }
    #endregion

    #region MiGoSacrifice
    private void MiGoSacrifice(Entity<MiGoComponent> uid, ref MiGoSacrificeEvent args)
    {
        if (!uid.Comp.IsPhysicalForm)
        {
            _popup.PopupClient(Loc.GetString("cult-yogg-cant-sacrafice-in-astral"), uid);
            return;
        }
        var altarQuery = EntityQueryEnumerator<CultYoggAltarComponent, TransformComponent>();

        while (altarQuery.MoveNext(out var altarUid, out var altarComp, out _))
        {
            if (!_transform.InRange(Transform(uid).Coordinates, Transform(altarUid).Coordinates, altarComp.RitualStartRange))
                continue;

            if (!TryComp<StrapComponent>(altarUid, out var strapComp))
                continue;

            if (strapComp.BuckledEntities.Count == 0)
                continue;

            if (!HasComp<CultYoggSacrificialComponent>(strapComp.BuckledEntities.First()))
                continue;

            TryDoSacrifice(altarUid, uid, altarComp);
        }
    }
    public bool TryDoSacrifice(EntityUid altarUid, EntityUid user, CultYoggAltarComponent altarComp)
    {
        if (altarComp == null)
            return false;

        if (!TryComp<StrapComponent>(altarUid, out var strapComp))
            return false;

        var targetUid = strapComp.BuckledEntities.FirstOrDefault();
        var migoQuery = EntityQueryEnumerator<MiGoComponent>();
        var currentMiGoAmount = 0;

        while (migoQuery.MoveNext(out var migoUid, out var miGoComponent))
        {
            if (miGoComponent == null)
                continue;

            if (_transform.InRange(Transform(migoUid).Coordinates, Transform(altarUid).Coordinates, altarComp.RitualStartRange))
                currentMiGoAmount++;
        }

        if (currentMiGoAmount < altarComp.RequiredAmountMiGo)
        {
            _popup.PopupClient(Loc.GetString("cult-yogg-altar-not-enough-migo"), user, user);

            return false;
        }

        var sacrificeDoAfter = new DoAfterArgs(EntityManager, user, altarComp.RutualTime, new MiGoSacrificeDoAfterEvent(), altarUid, target: targetUid)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            BlockDuplicate = true,
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent
        };

        var started = _doAfter.TryStartDoAfter(sacrificeDoAfter);

        if (started)
        {
            _popup.PopupPredicted(Loc.GetString("cult-yogg-sacrifice-started", ("user", user), ("target", targetUid)),
                altarUid, null, PopupType.MediumCaution);
        }

        return started;
    }

    #endregion

    #region Astral
    public override void Update(float delta)
    {
        base.Update(delta);
        var query = EntityQueryEnumerator<MiGoComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (IsPaused(uid))
                continue;

            if (comp.MaterializationTime == null)
                continue;

            var secondsLeft = (FixedPoint2)Math.Round((comp.MaterializationTime.Value - _timing.CurTime).TotalSeconds);//calculate time left in seconds

            if (comp.AlertTime == 0 || comp.AlertTime > secondsLeft)//update alert if buffer has a different value
            {
                comp.AlertTime = secondsLeft;
                _alerts.ShowAlert(uid, comp.AstralAlert);
            }

            if (_timing.CurTime <= comp.MaterializationTime.Value)
                continue;

            ChangeForm(uid, comp, true);
            if (!comp.AudioPlayed)
            {
                _audio.PlayPredicted(comp.SoundMaterialize, uid, uid, AudioParams.Default.WithMaxDistance(0.5f));
                comp.AudioPlayed = true;
            }
            _actions.StartUseDelay(comp.MiGoAstralActionEntity);
        }
    }
    private void MiGoAstral(Entity<MiGoComponent> uid, ref MiGoAstralEvent args)
    {
        if (!uid.Comp.IsPhysicalForm)
        {
            var doafterArgs = new DoAfterArgs(EntityManager, uid, uid.Comp.ExitingAstralDoAfter, new AfterMaterialize(), uid)
            {
                Broadcast = false,
                BreakOnDamage = false,
                NeedHand = false,
                BlockDuplicate = true,
                CancelDuplicate = false
            };

            var started = _doAfter.TryStartDoAfter(doafterArgs);
        }
        else
        {
            var doafterArgs = new DoAfterArgs(EntityManager, uid, uid.Comp.EnteringAstralDoAfter, new AfterDeMaterialize(), uid)
            {
                Broadcast = false,
                BreakOnDamage = false,
                NeedHand = false,
                BlockDuplicate = true,
                CancelDuplicate = false
            };

            var started = _doAfter.TryStartDoAfter(doafterArgs);
            if (started)
            {
                _audio.PlayPredicted(uid.Comp.SoundDeMaterialize, uid, uid, AudioParams.Default.WithMaxDistance(0.5f));
            }
        }
    }
    private void OnAfterMaterialize(Entity<MiGoComponent> uid, ref AfterMaterialize args)
    {
        if (args.Cancelled)
            return;

        if (args.Handled)
            return;

        args.Handled = true;

        _physics.SetBodyType(uid, BodyType.KinematicController);
        _audio.PlayPredicted(uid.Comp.SoundMaterialize, uid, uid, AudioParams.Default.WithMaxDistance(0.5f));

        ChangeForm(uid, uid.Comp, true);
        _actions.StartUseDelay(uid.Comp.MiGoAstralActionEntity);
        Dirty(uid);
    }

    private void OnAfterDeMaterialize(Entity<MiGoComponent> uid, ref AfterDeMaterialize args)
    {
        if (args.Cancelled)
            return;

        if (args.Handled)
            return;

        args.Handled = true;
        ChangeForm(uid, uid.Comp, false);
        uid.Comp.MaterializationTime = _timing.CurTime + uid.Comp.AstralDuration;

        var cooldownStart = _timing.CurTime;
        var cooldownEnd = cooldownStart + uid.Comp.CooldownAfterDematerialize;

        _actions.SetCooldown(uid.Comp.MiGoAstralActionEntity, cooldownStart, cooldownEnd);

        Dirty(uid);
    }

    public virtual void ChangeForm(EntityUid uid, MiGoComponent comp, bool isMaterial)
    {
        if (TryComp<FixturesComponent>(uid, out var fixtures) && fixtures.FixtureCount >= 1)
        {
            var fixture = fixtures.Fixtures.First();

            var mask = (int)(isMaterial ? CollisionGroup.FlyingMobMask : CollisionGroup.GhostImpassable);
            var layer = (int)(isMaterial ? CollisionGroup.FlyingMobLayer : CollisionGroup.None);

            _physics.SetCollisionMask(uid, fixture.Key, fixture.Value, mask, fixtures);
            _physics.SetCollisionLayer(uid, fixture.Key, fixture.Value, layer, fixtures);
        }

        //full vision during astral
        if (TryComp<EyeComponent>(uid, out var eye))
        {
            _eye.SetDrawFov(uid, isMaterial, eye);
            _eye.SetDrawLight((uid, eye), isMaterial);
        }
    }


    private void CheckAct(Entity<MiGoComponent> uid, ref AttackAttemptEvent args)
    {
        if (!uid.Comp.IsPhysicalForm)
            args.Cancel();
    }

    //ToDo check if its required

    private void OnGettingPickedUpAttempt(Entity<MiGoComponent> uid, ref GettingPickedUpAttemptEvent args)
    {
        if (!uid.Comp.IsPhysicalForm)
            args.Cancel();
    }

    private void OnDropAttempt(Entity<MiGoComponent> uid, ref DropAttemptEvent args)
    {
        if (!uid.Comp.IsPhysicalForm)
            args.Cancel();
    }
    private void OnBeingUsedAttempt(Entity<MiGoComponent> uid, ref GettingUsedAttemptEvent args)
    {
        if (!uid.Comp.IsPhysicalForm)
            args.Cancel();
    }
    private void OnThrowAttempt(Entity<MiGoComponent> uid, ref ThrowAttemptEvent args)
    {
        if (!uid.Comp.IsPhysicalForm)
            args.Cancel();
    }
    #endregion

    #region Enslave
    private void OnMiGoEnslaveAction(Entity<MiGoComponent> entity, ref MiGoEnslavementActionEvent args)
    {
        if (args.Handled)
            return;

        var (uid, comp) = entity;
        if (!comp.IsPhysicalForm)
            return;

        var target = args.Target;
        if (!CanEnslaveTarget(entity, target, out var reason))
        {
            _popup.PopupClient(reason, target, uid);
            return;
        }

        StartEnslaveDoAfter(entity, target);
        args.Handled = true;
    }

    protected void StartEnslaveDoAfter(Entity<MiGoComponent> entity, EntityUid target)
    {
        var (uid, comp) = entity;

        var doafterArgs = new DoAfterArgs(EntityManager, uid, comp.EnslaveTime, new MiGoEnslaveDoAfterEvent(), uid, target)//ToDo estimate time for Enslave
        {
            Broadcast = false,
            BreakOnDamage = true,
            BreakOnMove = false,
            NeedHand = false,
            BlockDuplicate = true,
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent
        };

        _doAfter.TryStartDoAfter(doafterArgs);
        _audio.PlayPredicted(comp.EnslavingSound, target, target);
    }

    protected bool CanEnslaveTarget(Entity<MiGoComponent> entity, EntityUid target, out string? reason)
    {
        var (uid, comp) = entity;
        reason = null;

        if (!HasComp<HumanoidAppearanceComponent>(target))
        {
            reason = Loc.GetString("cult-yogg-enslave-must-be-human");
            return false;
        }

        if (!_mobState.IsAlive(target))
        {
            reason = Loc.GetString("cult-yogg-enslave-must-be-alive");
            return false;
        }

        if (HasComp<RevolutionaryComponent>(target) || HasComp<MindShieldComponent>(target) || HasComp<ZombieComponent>(target))
        {
            reason = Loc.GetString("cult-yogg-enslave-another-fraction");
            return false;
        }

        if (!_statusEffectsSystem.HasStatusEffect(target, comp.RequiedEffect))
        {
            reason = Loc.GetString("cult-yogg-enslave-should-eat-shroom");
            return false;
        }

        if (HasComp<CultYoggSacrificialComponent>(target))
        {
            reason = Loc.GetString("cult-yogg-enslave-is-sacraficial");
            return false;
        }

        if (_mind.TryGetMind(target, out var mindId, out _))
        {
            if (TryComp<MindRoleComponent>(mindId, out var role) &&
                role.JobPrototype is { } job && job == "Chaplain")
            {
                reason = "cult-yogg-enslave-cant-be-a-chaplain";
                return false;
            }
        }
        else
        {
            if (_net.IsServer) // ToDo delete this check after MindContainer fixes
                reason = Loc.GetString("cult-yogg-no-mind");
            return false;
        }

        return true;
    }
    #endregion
}

[Serializable, NetSerializable]
public sealed partial class MiGoSacrificeDoAfterEvent : SimpleDoAfterEvent { }

[Serializable, NetSerializable]
public sealed partial class MiGoEnslaveDoAfterEvent : SimpleDoAfterEvent { }

[Serializable, NetSerializable]
public sealed partial class AfterMaterialize : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class AfterDeMaterialize : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}

[ByRefEvent, Serializable]
public record struct CultYoggEnslavedEvent(EntityUid? Target);

[NetSerializable, Serializable]
public enum MiGoTimerVisualLayers : byte
{
    Digit1,
    Digit2
}
[Serializable, NetSerializable]
public enum MiGoVisual
{
    Base,
    Astral
}
