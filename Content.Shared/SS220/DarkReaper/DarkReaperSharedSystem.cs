// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Explosion.Components;
using Content.Shared.Ghost;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Linq;
using System.Numerics;

namespace Content.Shared.SS220.DarkReaper;

public abstract class SharedDarkReaperSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifier = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly PullingSystem _puller = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DarkReaperComponent, ComponentStartup>(OnCompInit);
        SubscribeLocalEvent<DarkReaperComponent, ComponentShutdown>(OnCompShutdown);

        // actions
        SubscribeLocalEvent<DarkReaperComponent, ReaperRoflEvent>(OnRoflAction);
        SubscribeLocalEvent<DarkReaperComponent, ReaperConsumeEvent>(OnConsumeAction);
        SubscribeLocalEvent<DarkReaperComponent, ReaperMaterializeEvent>(OnMaterializeAction);
        SubscribeLocalEvent<DarkReaperComponent, ReaperStunEvent>(OnStunAction);
        SubscribeLocalEvent<DarkReaperComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<DarkReaperComponent, GetMeleeDamageEvent>(OnGetMeleeDamage);
        SubscribeLocalEvent<DarkReaperComponent, DamageModifyEvent>(OnDamageModify);
        SubscribeLocalEvent<DarkReaperComponent, ReaperBloodMistEvent>(OnBloodMistAction);
        SubscribeLocalEvent<DarkReaperComponent, ExamineAttemptEvent>(OnExamineAttempt);

        SubscribeLocalEvent<DarkReaperComponent, AfterMaterialize>(OnAfterMaterialize);
        SubscribeLocalEvent<DarkReaperComponent, AfterDeMaterialize>(OnAfterDeMaterialize);
        SubscribeLocalEvent<DarkReaperComponent, AfterConsumed>(OnAfterConsumed);
    }

    private readonly ProtoId<TagPrototype> _doorBumpOpenerTag = "DoorBumpOpener";
    private readonly ProtoId<NpcFactionPrototype> _simpleHostileFraction = "SimpleHostile";
    private readonly ProtoId<NpcFactionPrototype> _darkReaperPassive = "DarkReaperPassive";

    // Action bindings
    private void OnRoflAction(Entity<DarkReaperComponent> ent, ref ReaperRoflEvent args)
    {
        args.Handled = true;

        DoRoflAbility(ent, ent.Comp);
    }

    private void OnBloodMistAction(Entity<DarkReaperComponent> ent, ref ReaperBloodMistEvent args)
    {
        if (!ent.Comp.PhysicalForm)
        {
            _popup.PopupPredictedCursor(Loc.GetString("dark-reaper-cant-use-ability-in-non-physical-form"), ent);
            return;
        }

        args.Handled = true;
        _audio.PlayPredicted(args.BloodMistSound, ent, ent);
        Spawn(args.BloodMistProto, Transform(ent).Coordinates);
    }

    private void OnConsumeAction(Entity<DarkReaperComponent> ent, ref ReaperConsumeEvent args)
    {
        if (!ent.Comp.PhysicalForm)
        {
            _popup.PopupPredictedCursor(Loc.GetString("dark-reaper-cant-use-ability-in-non-physical-form"), ent);
            return;
        }

        // Only consume dead
        if (!_mobState.IsDead(args.Target))
        {
            if (_net.IsClient && _timing.IsFirstTimePredicted)
                _popup.PopupEntity("Цель должна быть мертва!", ent, PopupType.MediumCaution);
            return;
        }

        if (!TryComp<HumanoidAppearanceComponent>(args.Target, out _))
        {
            if (_net.IsClient && _timing.IsFirstTimePredicted)
                _popup.PopupEntity("Цель должна быть гуманоидом!", ent, PopupType.MediumCaution);
            return;
        }

        //Dark Reaper consume fix begin
        if (HasComp<CannotBeConsumedComponent>(args.Target))
        {
            if (_net.IsClient && _timing.IsFirstTimePredicted)
                _popup.PopupEntity("Невозможно поглотить", ent, PopupType.MediumCaution);
            return;
        }
        //Dark Reaper consume fix end

        var doafterArgs = new DoAfterArgs(
            EntityManager,
            ent,
            TimeSpan.FromSeconds(9 /* Hand-picked value to match the sound */),
            new AfterConsumed(),
            ent,
            args.Target
        )
        {
            Broadcast = false,
            BreakOnDamage = false,
            BreakOnMove = true,
            NeedHand = false,
            BlockDuplicate = true,
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent
        };

        var started = _doAfter.TryStartDoAfter(doafterArgs);
        if (started)
        {
            ent.Comp.ConsoomAudio = _audio.PlayPredicted(ent.Comp.ConsumeAbilitySound, ent, ent)?.Entity;
        }
    }

    private void OnMaterializeAction(Entity<DarkReaperComponent> ent, ref ReaperMaterializeEvent args)
    {
        DoMaterialize(ent);
    }

    private void OnStunAction(Entity<DarkReaperComponent> ent, ref ReaperStunEvent args)
    {
        if (!ent.Comp.PhysicalForm)
        {
            _popup.PopupPredictedCursor(Loc.GetString("dark-reaper-cant-use-ability-in-non-physical-form"), ent);
            return;
        }

        args.Handled = true;
        DoStunAbility(ent);
    }

    // Actions
    protected virtual void DoStunAbility(Entity<DarkReaperComponent> entity)
    {
        _audio.PlayPredicted(entity.Comp.StunAbilitySound, entity, entity);
        entity.Comp.StunScreamStart = _timing.CurTime;
        Dirty(entity);
        _appearance.SetData(entity, DarkReaperVisual.StunEffect, true);

        var entitiesNearReaper = _lookup.GetEntitiesInRange(entity, entity.Comp.StunAbilityRadius);
        foreach (var entityNearReaper in entitiesNearReaper)
        {
            _stun.TryUpdateParalyzeDuration(entityNearReaper, entity.Comp.StunDuration);
        }

        var confusedEntities = _lookup.GetEntitiesInRange(entity, entity.Comp.StunAbilityConfusion);
        foreach (var confusedEntity in confusedEntities)
        {
            _statusEffectsSystem.TryUpdateStatusEffectDuration(confusedEntity, entity.Comp.ConfusionEffectName, out _, entity.Comp.ConfusionDuration);
        }
    }

    protected virtual void DoRoflAbility(EntityUid uid, DarkReaperComponent comp)
    {
        _audio.PlayPredicted(comp.RolfAbilitySound, uid, uid);
    }

    protected void DoMaterialize(Entity<DarkReaperComponent> entity)
    {
        if (!entity.Comp.PhysicalForm)
        {
            var doafterArgs = new DoAfterArgs(
                EntityManager,
                entity,
                TimeSpan.FromSeconds(1.25 /* Hand-picked value to match the sound */),
                new AfterMaterialize(),
                entity
            )
            {
                Broadcast = false,
                BreakOnDamage = false,
                BreakOnMove = false,
                NeedHand = false,
                BlockDuplicate = true,
                CancelDuplicate = false
            };

            var started = _doAfter.TryStartDoAfter(doafterArgs);
            if (started)
            {
                _physics.SetBodyType(entity, BodyType.Static);
                _audio.PlayPredicted(entity.Comp.PortalOpenSound, entity, entity);
            }
        }
        else
        {
            var doafterArgs = new DoAfterArgs(
                EntityManager,
                entity,
                TimeSpan.FromSeconds(4.14 /* Hand-picked value to match the sound */),
                new AfterDeMaterialize(),
                entity
            )
            {
                Broadcast = false,
                BreakOnDamage = false,
                BreakOnMove = false,
                NeedHand = false,
                BlockDuplicate = true,
                CancelDuplicate = false
            };

            var started = _doAfter.TryStartDoAfter(doafterArgs);
            if (started)
            {
                _audio.PlayPredicted(entity.Comp.PortalCloseSound, entity, entity);
            }
        }
    }

    protected virtual void OnAfterConsumed(Entity<DarkReaperComponent> ent, ref AfterConsumed args)
    {
        args.Handled = true;

        if (ent.Comp.ConsoomAudio == null)
            return;

        ent.Comp.ConsoomAudio = _audio.Stop(ent.Comp.ConsoomAudio);
        ent.Comp.ConsoomAudio = null;
    }

    private void OnAfterMaterialize(Entity<DarkReaperComponent> ent, ref AfterMaterialize args)
    {
        args.Handled = true;

        _physics.SetBodyType(ent, BodyType.KinematicController);

        if (!args.Cancelled)
        {
            ChangeForm(ent, true);
            ent.Comp.MaterializedStart = _timing.CurTime;

            var cooldownStart = _timing.CurTime;
            var cooldownEnd = cooldownStart + ent.Comp.CooldownAfterMaterialize;

            _actions.SetCooldown(ent.Comp.MaterializeActionEntity, cooldownStart, cooldownEnd);

            if (_net.IsServer)
            {
                CreatePortal(ent);
            }
        }
    }

    protected virtual void CreatePortal(Entity<DarkReaperComponent> entity)
    {
        if (_prototype.HasIndex<EntityPrototype>(entity.Comp.PortalEffectPrototype))
        {
            var portalEntity = Spawn(entity.Comp.PortalEffectPrototype, Transform(entity).Coordinates);
            entity.Comp.ActivePortal = portalEntity;
        }
    }

    private void OnAfterDeMaterialize(Entity<DarkReaperComponent> ent, ref AfterDeMaterialize args)
    {
        args.Handled = true;

        if (args.Cancelled)
            return;

        ChangeForm(ent, false);
        _actions.StartUseDelay(ent.Comp.MaterializeActionEntity);
    }

    // Update loop
    public override void Update(float delta)
    {
        base.Update(delta);

        var query = EntityQueryEnumerator<DarkReaperComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (IsPaused(uid))
                continue;

            if (_net.IsServer && TryComp<ActionComponent>(comp.MaterializeActionEntity, out var materializeData))
            {
                var visibleEyes = materializeData.Cooldown.HasValue &&
                                       materializeData.Cooldown.Value.End > _timing.CurTime &&
                                       !comp.PhysicalForm;

                _appearance.SetData(uid, DarkReaperVisual.GhostCooldown, visibleEyes);
            }

            if (comp.StunScreamStart != null)
            {
                if (comp.StunScreamStart.Value + comp.StunGlareLength < _timing.CurTime)
                {
                    comp.StunScreamStart = null;
                    Dirty(uid, comp);
                    _appearance.SetData(uid, DarkReaperVisual.StunEffect, false);
                }
                else
                {
                    _appearance.SetData(uid, DarkReaperVisual.StunEffect, true);
                }
            }

            if (comp.MaterializedStart != null)
            {
                var maxDuration = comp.MaterializeDurations[comp.CurrentStage - 1];
                var diff = comp.MaterializedStart.Value + maxDuration - _timing.CurTime;
                if (diff.TotalSeconds < 4.14 && comp.PlayingPortalAudio == null)
                {
                    comp.PlayingPortalAudio = _audio.PlayPredicted(comp.PortalCloseSound, uid, uid)?.Entity;
                }

                if (diff > TimeSpan.Zero)
                    continue;

                ChangeForm((uid, comp), false);
                _actions.StartUseDelay(comp.MaterializeActionEntity);
            }
            else
            {
                comp.PlayingPortalAudio = null;
            }
        }
    }

    // Crap
    protected virtual void OnCompInit(Entity<DarkReaperComponent> ent, ref ComponentStartup args)
    {
        UpdateStageAppearance(ent, ent.Comp);
        ChangeForm(ent, ent.Comp.PhysicalForm);

        _pointLight.SetEnabled(ent, ent.Comp.StunScreamStart.HasValue);

        // Make tests crash & burn if stupid things are done
        DebugTools.Assert(ent.Comp.MaxStage >= 1, "DarkReaperComponent.MaxStage must always be equal or greater than 1.");
    }

    protected virtual void OnCompShutdown(Entity<DarkReaperComponent> ent, ref ComponentShutdown args)
    {
    }

    public virtual void ChangeForm(Entity<DarkReaperComponent> entity, bool isMaterial)
    {
        var (uid, comp) = entity;
        comp.PhysicalForm = isMaterial;

        if (TryComp<FixturesComponent>(uid, out var fixturesComp))
        {
            if (fixturesComp.Fixtures.TryGetValue("fix1", out var fixture))
            {
                var mask = (int)(isMaterial ? CollisionGroup.MobMask : CollisionGroup.None);
                var layer = (int)(isMaterial ? CollisionGroup.MobLayer : CollisionGroup.GhostImpassable);
                _physics.SetCollisionMask(uid, "fix1", fixture, mask);
                _physics.SetCollisionLayer(uid, "fix1", fixture, layer);
            }
        }

        if (TryComp<EyeComponent>(uid, out var eye))
            _eye.SetDrawFov(uid, isMaterial, eye);

        _appearance.SetData(uid, DarkReaperVisual.PhysicalForm, isMaterial);

        if (isMaterial)
        {
            EnsureComp<PullerComponent>(uid).NeedsHands = false;
            _tag.AddTag(uid, _doorBumpOpenerTag);

            if (TryComp<ExplosionResistanceComponent>(uid, out var explosionResistanceComponent))
                explosionResistanceComponent.DamageCoefficient = 1f; //full damage

            if (HasComp<NpcFactionMemberComponent>(uid))
            {
                _npcFaction.ClearFactions(uid);
                _npcFaction.AddFaction(uid, _simpleHostileFraction);
            }
        }
        else
        {
            _tag.RemoveTag(uid, _doorBumpOpenerTag);
            comp.StunScreamStart = null;
            comp.MaterializedStart = null;

            if (TryComp<ExplosionResistanceComponent>(uid, out var explodeComponent))
                explodeComponent.DamageCoefficient = 0f; // full resistance

            if (HasComp<NpcFactionMemberComponent>(uid))
            {
                _npcFaction.ClearFactions(uid);
                _npcFaction.AddFaction(uid, _darkReaperPassive);
            }
            _appearance.SetData(uid, DarkReaperVisual.StunEffect, false);

            if (TryComp(uid, out PullerComponent? puller) && TryComp(puller.Pulling, out PullableComponent? pullable))
                _puller.TryStopPull(puller.Pulling.Value, pullable);

            RemComp<PullerComponent>(uid);
            RemComp<ActivePullerComponent>(uid);
        }

        _actions.SetEnabled(comp.StunActionEntity, isMaterial);
        _actions.SetEnabled(comp.ConsumeActionEntity, isMaterial);

        ToggleWeapon(uid, comp, isMaterial);
        UpdateMovementSpeed(uid, comp);

        Dirty(uid, comp);
    }

    public void ChangeStage(EntityUid uid, DarkReaperComponent comp, int stage)
    {
        comp.CurrentStage = stage;
        UpdateStageAppearance(uid, comp);
    }

    public void UpdateStage(EntityUid uid, DarkReaperComponent comp)
    {
        if (!comp.ConsumedPerStage.TryGetValue(comp.CurrentStage - 1, out var nextStageReq))
            return;

        if (comp.Consumed < nextStageReq)
            return;

        comp.Consumed = 0;
        ChangeStage(uid, comp, comp.CurrentStage + 1);
        _audio.PlayPredicted(comp.LevelupSound, uid, uid);
    }

    private void UpdateStageAppearance(EntityUid uid, DarkReaperComponent comp)
    {
        _appearance.SetData(uid, DarkReaperVisual.Stage, comp.CurrentStage);
    }

    // This cursed shit exists because we can't disable components.
    private void ToggleWeapon(EntityUid uid, DarkReaperComponent comp, bool isEnabled)
    {
        if (!_net.IsServer)
            return;

        if (!isEnabled)
        {
            if (TryComp<MeleeWeaponComponent>(uid, out var weapon))
                RemComp(uid, weapon);
        }
        else
        {
            var weapon = EnsureComp<MeleeWeaponComponent>(uid);
            weapon.Hidden = true;
            weapon.Angle = 0;
            weapon.Animation = "WeaponArcClaw";
            weapon.HitSound = comp.HitSound;
            weapon.SwingSound = comp.SwingSound;
        }
    }

    private void OnGetMeleeDamage(Entity<DarkReaperComponent> ent, ref GetMeleeDamageEvent args)
    {
        if (!ent.Comp.PhysicalForm ||
            !ent.Comp.StageMeleeDamage.TryGetValue(ent.Comp.CurrentStage - 1, out var damageSet))
            damageSet = new();

        args.Damage = new()
        {
            DamageDict = damageSet,
        };
    }

    private void OnDamageModify(Entity<DarkReaperComponent> ent, ref DamageModifyEvent args)
    {
        if (!ent.Comp.PhysicalForm)
        {
            args.Damage = new();
        }
        else
        {
            if (!ent.Comp.StageDamageResists.TryGetValue(ent.Comp.CurrentStage, out var resists))
                return;

            args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, resists);
        }
    }

    private void UpdateMovementSpeed(EntityUid uid, DarkReaperComponent comp)
    {
        if (!TryComp<MovementSpeedModifierComponent>(uid, out var modifComp))
            return;

        var speed = comp.PhysicalForm ? comp.MaterialMovementSpeed : comp.UnMaterialMovementSpeed;
        _speedModifier.ChangeBaseSpeed(uid, speed, speed, modifComp.Acceleration, modifComp);
    }

    private void OnMobStateChanged(Entity<DarkReaperComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        ent.Comp.ConsoomAudio = _audio.Stop(ent.Comp.ConsoomAudio);
        ent.Comp.PlayingPortalAudio = _audio.Stop(ent.Comp.PlayingPortalAudio);

        if (!_net.IsServer)
            return;

        QueueDel(ent.Comp.ActivePortal);

        // play at coordinates because entity is getting deleted
        var coordinates = Transform(ent).Coordinates;
        _audio.PlayPvs(ent.Comp.SoundDeath, coordinates);

        // Get everthing that was consumed out before deleting
        if (_container.TryGetContainer(ent, DarkReaperComponent.ConsumedContainerId, out var container))
            _container.EmptyContainer(container);

        // Make it blow up on pieces after deth
        var gibPoolAsArray = ent.Comp.SpawnOnDeathPool.ToArray();
        var goreAmountToSpawn = ent.Comp.SpawnOnDeathAmount + ent.Comp.SpawnOnDeathAdditionalPerStage * (ent.Comp.CurrentStage - 1);

        var goreSpawnCoords = Transform(ent).Coordinates;
        for (var i = 0; i < goreAmountToSpawn; i++)
        {
            var protoToSpawn = gibPoolAsArray[_random.Next(gibPoolAsArray.Length)];
            var goreEntity = Spawn(protoToSpawn, goreSpawnCoords);

            _transform.SetLocalRotationNoLerp(goreEntity, Angle.FromDegrees(_random.NextDouble(0, 360)));

            var maxAxisImp = ent.Comp.SpawnOnDeathImpulseStrength;
            var impulseVec = new Vector2(_random.NextFloat(-maxAxisImp, maxAxisImp), _random.NextFloat(-maxAxisImp, maxAxisImp));
            _physics.ApplyLinearImpulse(goreEntity, impulseVec);
        }

        // insallah
        QueueDel(ent);
    }

    private void OnExamineAttempt(Entity<DarkReaperComponent> ent, ref ExamineAttemptEvent args)//Won't be required if we redo reaper on invisibility system
    {
        if (HasComp<GhostComponent>(args.Examiner))
            return;

        if (ent.Comp.PhysicalForm)
            return;

        args.Cancel();
        return;
    }
}

