// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Actions;
using Content.Shared.Zombies;
using Content.Shared.Mind;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Mindshield.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.DoAfter;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Content.Shared.Physics;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Content.Shared.Tag;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Shared.Serialization;
using Content.Shared.StatusEffect;
using Robust.Shared.Timing;
using Content.Shared.NPC.Systems;
using Content.Shared.SS220.CultYogg.Components;
using Content.Shared.Mobs;
using Content.Shared.Buckle.Components;
using System.Linq;

namespace Content.Shared.SS220.CultYogg.EntitySystems;

public abstract class SharedMiGoSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifier = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    //[Dependency] private readonly CultYoggRuleSystem _cultYoggRule = default!; //maybe use this for enslavement

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MiGoComponent, ComponentStartup>(OnCompInit);

        // actions
        SubscribeLocalEvent<MiGoComponent, MiGoEnslavementEvent>(MiGoEnslave);
        SubscribeLocalEvent<MiGoComponent, MiGoAstralEvent>(MiGoAstral);
        SubscribeLocalEvent<MiGoComponent, MiGoHealEvent>(MiGoHeal);
        SubscribeLocalEvent<MiGoComponent, MiGoErectEvent>(MiGoErect);
        SubscribeLocalEvent<MiGoComponent, MiGoSacrificeEvent>(MiGoSacrifice);


        SubscribeLocalEvent<MiGoComponent, BoundUIOpenedEvent>(OnBoundUIOpened);

        SubscribeLocalEvent<MiGoComponent, AfterMaterialize>(OnAfterMaterialize);
        SubscribeLocalEvent<MiGoComponent, AfterDeMaterialize>(OnAfterDeMaterialize);
    }

    protected virtual void OnCompInit(Entity<MiGoComponent> uid, ref ComponentStartup args)
    {
        _actions.AddAction(uid, ref uid.Comp.MiGoHealActionEntity, uid.Comp.MiGoHealAction);
        _actions.AddAction(uid, ref uid.Comp.MiGoEnslavementActionEntity, uid.Comp.MiGoEnslavementAction);
        _actions.AddAction(uid, ref uid.Comp.MiGoAstralActionEntity, uid.Comp.MiGoAstralAction);
        _actions.AddAction(uid, ref uid.Comp.MiGoErectActionEntity, uid.Comp.MiGoErectAction);
        _actions.AddAction(uid, ref uid.Comp.MiGoSacrificeActionEntity, uid.Comp.MiGoSacrificeAction);
    }
    #region Enslave
    private void MiGoEnslave(Entity<MiGoComponent> uid, ref MiGoEnslavementEvent args)
    {
        if (args.Handled)
            return;

        if (!_mind.TryGetMind(args.Target, out var mindId, out var mind))
        {
            if (_net.IsClient)
                _popup.PopupEntity(Loc.GetString("cult-yogg-no-mind"), args.Target, uid);
            return;
        }

        if (!HasComp<HumanoidAppearanceComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("cult-yogg-enslave-must-be-human"), args.Target, uid);
            return;
        }

        if (!_mobState.IsAlive(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("cult-yogg-enslave-must-be-alive"), args.Target, uid);
            return;
        }

        if (HasComp<RevolutionaryComponent>(args.Target) || HasComp<MindShieldComponent>(args.Target) || HasComp<ZombieComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("cult-yogg-enslave-another-fraction"), args.Target, uid);
            return;
        }

        if (!_statusEffectsSystem.HasStatusEffect(args.Target, uid.Comp.RequiedEffect))
        {
            _popup.PopupEntity(Loc.GetString("cult-yogg-enslave-should-eat-shroom"), args.Target, uid);
            return;
        }

        if (HasComp<CultYoggSacrificialComponent>(uid))
        {
            if (_net.IsClient)
                _popup.PopupEntity(Loc.GetString("cult-yogg-enslave-is-sacraficial"), args.Target, uid);
            return;
        }

        var doafterArgs = new DoAfterArgs(EntityManager, uid, TimeSpan.FromSeconds(3), new MiGoEnslaveDoAfterEvent(), uid, args.Target)//ToDo estimate time for Enslave
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

        args.Handled = true;
    }
    #endregion

    #region Astral
    private void MiGoAstral(Entity<MiGoComponent> uid, ref MiGoAstralEvent args)
    {
        if (!uid.Comp.PhysicalForm)
        {
            var doafterArgs = new DoAfterArgs(
                EntityManager,
                uid,
                TimeSpan.FromSeconds(1.25 /* Hand-picked value to match the sound */),
                new AfterMaterialize(),
                uid
            )
            {
                Broadcast = false,
                BreakOnDamage = false,
                //BreakOnTargetMove = false,
                //BreakOnUserMove = false,
                NeedHand = false,
                BlockDuplicate = true,
                CancelDuplicate = false
            };

            var started = _doAfter.TryStartDoAfter(doafterArgs);
            if (started)
            {
                _physics.SetBodyType(uid, BodyType.Static);
                //_audio.PlayPredicted(comp.PortalOpenSound, uid, uid); //ToDo Our own sound
            }
        }
        else
        {
            var doafterArgs = new DoAfterArgs(
                EntityManager,
                uid,
                TimeSpan.FromSeconds(1.25 /* Hand-picked value to match the sound */),
                new AfterDeMaterialize(),
                uid
            )
            {
                Broadcast = false,
                BreakOnDamage = false,
                //BreakOnTargetMove = false,
                //BreakOnUserMove = false,
                NeedHand = false,
                BlockDuplicate = true,
                CancelDuplicate = false
            };

            var started = _doAfter.TryStartDoAfter(doafterArgs);
            if (started)
            {
                //_audio.PlayPredicted(comp.PortalCloseSound, uid, uid); //ToDo Our own sound
            }
        }

        //ToDo https://github.com/TheArturZh/space-station-14/blob/b0ee614751216474ddbeabab970b3ab505f63845/Content.Shared/SS220/DarkReaper/DarkReaperSharedSystem.cs#L4
    }
    private void OnAfterMaterialize(Entity<MiGoComponent> uid, ref AfterMaterialize args)
    {
        args.Handled = true;

        _physics.SetBodyType(uid, BodyType.KinematicController);

        if (!args.Cancelled)
        {
            ChangeForm(uid, uid.Comp, true);

            _actions.StartUseDelay(uid.Comp.MiGoAstralActionEntity);
        }
    }

    private void OnAfterDeMaterialize(Entity<MiGoComponent> uid, ref AfterDeMaterialize args)
    {
        args.Handled = true;

        if (!args.Cancelled)
        {
            ChangeForm(uid, uid.Comp, false);
            uid.Comp.DeMaterializedStart = _timing.CurTime;

            var cooldownStart = _timing.CurTime;
            var cooldownEnd = cooldownStart + uid.Comp.CooldownAfterMaterialize;

            _actions.SetCooldown(uid.Comp.MiGoAstralActionEntity, cooldownStart, cooldownEnd);
        }
    }

    public virtual void ChangeForm(EntityUid uid, MiGoComponent comp, bool isMaterial)
    {
        comp.PhysicalForm = isMaterial;

        if (TryComp<FixturesComponent>(uid, out var fixturesComp))
        {
            if (fixturesComp.Fixtures.TryGetValue("fix1", out var fixture))
            {
                var mask = (int) (isMaterial ? CollisionGroup.MobMask : CollisionGroup.GhostImpassable);
                var layer = (int) (isMaterial ? CollisionGroup.MobLayer : CollisionGroup.None);
                _physics.SetCollisionMask(uid, "fix1", fixture, mask);
                _physics.SetCollisionLayer(uid, "fix1", fixture, layer);
            }
        }
        /*
        if (TryComp<EyeComponent>(uid, out var eye))
            _eye.SetDrawFov(uid, isMaterial, eye);
        _appearance.SetData(uid, DarkReaperVisual.PhysicalForm, isMaterial);
        */

        if (isMaterial)
        {
            _tag.AddTag(uid, "DoorBumpOpener");
            comp.DeMaterializedStart = null;
            /*
            if (HasComp<NpcFactionMemberComponent>(uid))
            {
                _npcFaction.ClearFactions(uid);
                _npcFaction.AddFaction(uid, "SimpleHostile");
            }
            */
        }
        else
        {
            _tag.RemoveTag(uid, "DoorBumpOpener");
            /*
            if (HasComp<NpcFactionMemberComponent>(uid))
            {
                _npcFaction.ClearFactions(uid);
                _npcFaction.AddFaction(uid, "DarkReaperPassive");
            }
            */
        }

        var ev = new MiGoAstralAppearanceEvent();
        RaiseLocalEvent(uid, ref ev);

        UpdateMovementSpeed(uid, comp);

        Dirty(uid, comp);
    }

    private void UpdateMovementSpeed(EntityUid uid, MiGoComponent comp)
    {
        if (!TryComp<MovementSpeedModifierComponent>(uid, out var modifComp))
            return;

        var speed = comp.PhysicalForm ? comp.MaterialMovementSpeed : comp.UnMaterialMovementSpeed;
        _speedModifier.ChangeBaseSpeed(uid, speed, speed, modifComp.Acceleration, modifComp);
    }
    // Update loop
    public override void Update(float delta)
    {
        base.Update(delta);

        var query = EntityQueryEnumerator<MiGoComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (IsPaused(uid))
                continue;

            if (_net.IsServer && _actions.TryGetActionData(comp.MiGoAstralActionEntity, out var materializeData, false))
            {
                //var visibleEyes = materializeData.Cooldown.HasValue && materializeData.Cooldown.Value.End > _timing.CurTime && !comp.PhysicalForm;
                //_appearance.SetData(uid, DarkReaperVisual.GhostCooldown, visibleEyes);
            }

            if (comp.DeMaterializedStart != null)
            {
                var maxDuration = comp.MaterializeDurations[2];
                var diff = comp.DeMaterializedStart.Value + maxDuration - _timing.CurTime;
                if (diff <= TimeSpan.Zero)
                {
                    ChangeForm(uid, comp, false);
                    _actions.StartUseDelay(comp.MiGoAstralActionEntity);
                }
            }
        }
    }
    #endregion

    #region Heal
    private void MiGoHeal(Entity<MiGoComponent> uid, ref MiGoHealEvent args)
    {
        if (args.Handled)
            return;

        //unregistred errors idk how to fix
        //_entityManager.System<SharedMiGoHealSystem>().TryApplyMiGoHeal(args.Target, uid.Comp.HealingEffectTime);

        var ev = new MiGoSetHealStatusEvent(args.Target, uid.Comp.HealingEffectTime);
        RaiseLocalEvent(uid, ref ev);

        args.Handled = true;
    }
    #endregion

    #region Erect
    private void MiGoErect(EntityUid uid, MiGoComponent comp, MiGoErectEvent args)
    {
        //(Entity<MiGoComponent> uid, ref MiGoErectEvent args)
        //will wait when sw will update ui parts to copy paste, cause rn it has an errors
        if (args.Handled || !TryComp<ActorComponent>(uid, out var actor))
            return;
        args.Handled = true;

        _userInterface.TryToggleUi(uid, MiGoErectUiKey.Key, actor.PlayerSession);
    }
    private void OnBoundUIOpened(EntityUid uid, MiGoComponent component, BoundUIOpenedEvent args)
    {
        //(Entity<MiGoComponent> uid, ref BoundUIOpenedEvent args)
        /*
        _entityManager.TryGetComponent<IntrinsicRadioTransmitterComponent>(uid, out var intrinsicRadio);
        var radioChannels = intrinsicRadio?.Channels;

        var state = new SiliconLawBuiState(GetLaws(uid).Laws, radioChannels);
        _userInterface.SetUiState(args.Entity, SiliconLawsUiKey.Key, state);
        */
    }
    #endregion

    #region MiGoSacrifice
    private void MiGoSacrifice(Entity<MiGoComponent> uid, ref MiGoSacrificeEvent args)
    {
        var altarQuery = EntityQueryEnumerator<CultYoggAltarComponent, TransformComponent>();

        while (altarQuery.MoveNext(out var altarUid, out var altarComp, out _))
        {
            if (!_transform.InRange(Transform(uid).Coordinates, Transform(altarUid).Coordinates, altarComp.RitualStartRange))
                continue;

            if (!TryComp<StrapComponent>(altarUid, out var strapComp))
                continue;

            if (!strapComp.BuckledEntities.Any())
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

            if (_transform.InRange(Transform(migoUid).Coordinates, Transform(altarUid).Coordinates,  altarComp.RitualStartRange))
                currentMiGoAmount++;
        }

        if (currentMiGoAmount < altarComp.RequiredAmountMiGo)
        {
            if (_net.IsServer)
                _popup.PopupEntity(Loc.GetString("cult-yogg-altar-not-enough-migo"), user, user);

            return false;
        }

        var sacrificeDoAfter = new DoAfterArgs(
        EntityManager,
        user,
        altarComp.RutualTime,
        new MiGoSacrificeDoAfterEvent(),
        altarUid,
        target: targetUid
        )
        {
            BreakOnDamage = true,
            BreakOnMove = true
        };

        var started = _doAfter.TryStartDoAfter(sacrificeDoAfter);

        if (started)
        {
            if (_net.IsServer)
                _popup.PopupEntity(Loc.GetString("cult-yogg-sacrifice-started", ("user", user), ("target", targetUid)),
                 altarUid, PopupType.MediumCaution);

            //_audio.PlayPredicted(altarComp.RitualSound, user, user); // TODO: ritual sound(mythic)
        }

        return started;
    }

    #endregion
}

[Serializable, NetSerializable]
public sealed partial class MiGoSacrificeDoAfterEvent : SimpleDoAfterEvent { }

[Serializable, NetSerializable]
public sealed partial class MiGoEnslaveDoAfterEvent : SimpleDoAfterEvent
{
}

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

[ByRefEvent]
public record struct MiGoAstralAppearanceEvent();
