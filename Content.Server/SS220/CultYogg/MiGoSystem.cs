// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Actions;
using Content.Shared.SS220.CultYogg.Components;
using Content.Shared.SS220.CultYogg.EntitySystems;
using Content.Server.SS220.GameTicking.Rules;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Mindshield.Components;
using Content.Shared.Revolutionary.Components;
using Content.Shared.SS220.CultYogg;
using Content.Shared.StatusEffect;
using Content.Shared.Zombies;
using Content.Shared.Mind;
using Content.Server.Bible.Components;
using Content.Shared.Popups;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Audio.Systems;
using Content.Shared.Eye;
using Content.Shared.Movement.Components;
using Content.Shared.Physics;
using Robust.Server.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Movement.Systems;
using Robust.Shared.Timing;
using Content.Shared.Tag;
using Content.Shared.Alert;
using JetBrains.FormatRipper.Elf;

namespace Content.Server.SS220.CultYogg;

public sealed partial class MiGoSystem : SharedMiGoSystem
{

    [Dependency] private readonly CultYoggRuleSystem _cultRule = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifier = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly VisibilitySystem _visibility = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedEyeSystem _eye = default!;
    public override void Initialize()
    {
        base.Initialize();

        //actions
        SubscribeLocalEvent<MiGoComponent, MiGoEnslavementEvent>(MiGoEnslave);
        SubscribeLocalEvent<MiGoComponent, MiGoAstralEvent>(MiGoAstral);

        SubscribeLocalEvent<MiGoComponent, MiGoEnslaveDoAfterEvent>(MiGoEnslaveOnDoAfter);

        //astral DoAfterEvents
        SubscribeLocalEvent<MiGoComponent, AfterMaterialize>(OnAfterMaterialize);
        SubscribeLocalEvent<MiGoComponent, AfterDeMaterialize>(OnAfterDeMaterialize);
    }

    #region Astral
    private void MiGoAstral(Entity<MiGoComponent> uid, ref MiGoAstralEvent args)
    {
        if (!uid.Comp.IsPhysicalForm)
        {
            var doafterArgs = new DoAfterArgs(EntityManager, uid, TimeSpan.FromSeconds(1.25), new AfterMaterialize(), uid)
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
                _physics.SetBodyType(uid, BodyType.Static);
                _audio.PlayEntity(uid.Comp.SoundMaterialize, uid, uid);
                //_audio.PlayPredicted(uid.Comp.SoundMaterialize, uid, uid);
            }
        }
        else
        {
            var doafterArgs = new DoAfterArgs(EntityManager, uid, TimeSpan.FromSeconds(1.25), new AfterDeMaterialize(), uid)
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
                _audio.PlayEntity(uid.Comp.SoundDeMaterialize, uid, uid);
                //_audio.PlayPredicted(uid.Comp.SoundDeMaterialize, uid, uid);
            }
        }
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
            uid.Comp.MaterializationTime = _timing.CurTime + uid.Comp.AstralDuration;

            var cooldownStart = _timing.CurTime;
            var cooldownEnd = cooldownStart + uid.Comp.CooldownAfterDematerialize;

            _actions.SetCooldown(uid.Comp.MiGoAstralActionEntity, cooldownStart, cooldownEnd);
        }
    }

    public void ChangeForm(EntityUid uid, MiGoComponent comp, bool isMaterial)
    {
        comp.IsPhysicalForm = isMaterial;

        if (TryComp<FixturesComponent>(uid, out var fixturesComp))
        {
            if (fixturesComp.Fixtures.TryGetValue("fix1", out var fixture))
            {
                var mask = (int)(isMaterial ? CollisionGroup.MobMask : CollisionGroup.GhostImpassable);
                var layer = (int)(isMaterial ? CollisionGroup.MobLayer : CollisionGroup.None);
                _physics.SetCollisionMask(uid, "fix1", fixture, mask);
                _physics.SetCollisionLayer(uid, "fix1", fixture, layer);
            }
        }

        //full vision during astral
        if (TryComp<EyeComponent>(uid, out var eye))
            _eye.SetDrawFov(uid, isMaterial, eye);

        if (!TryComp<VisibilityComponent>(uid, out var vis))
            return;

        if (isMaterial)
        {
            //no opening door during astral
            _tag.AddTag(uid, "DoorBumpOpener");
            comp.MaterializationTime = null;

            _alerts.ClearAlert(uid, comp.AstralAlert);

            //no phisyc during astral
            EnsureComp<MovementIgnoreGravityComponent>(uid);

            //some copypaste invisibility shit
            _visibility.AddLayer((uid, vis), (int)VisibilityFlags.Normal, false);
            _visibility.RemoveLayer((uid, vis), (int)VisibilityFlags.Ghost, false);

            //trying make migo transpartent visout sprite, like reaper
            _appearance.SetData(uid, MiGoVisual.Base, false);
            _appearance.RemoveData(uid, MiGoVisual.Astral);

            //for agro and turrets
            if (HasComp<NpcFactionMemberComponent>(uid))
            {
                _npcFaction.ClearFactions(uid);
                _npcFaction.AddFaction(uid, "CultYogg");
            }
        }
        else
        {
            comp.AudioPlayed = false;
            _tag.RemoveTag(uid, "DoorBumpOpener");

            _alerts.ShowAlert(uid, comp.AstralAlert);

            RemComp<MovementIgnoreGravityComponent>(uid);

            if (HasComp<NpcFactionMemberComponent>(uid))
            {
                _npcFaction.ClearFactions(uid);
                _npcFaction.AddFaction(uid, "SimpleNeutral");
            }
            _visibility.AddLayer((uid, vis), (int)VisibilityFlags.Ghost, false);
            _visibility.RemoveLayer((uid, vis), (int)VisibilityFlags.Normal, false);

            _appearance.SetData(uid, MiGoVisual.Astral, false);
            _appearance.RemoveData(uid, MiGoVisual.Base);
        }

        _visibility.RefreshVisibility(uid, vis);

        UpdateMovementSpeed(uid, comp);

        Dirty(uid, comp);
    }

    //moving in astral faster
    private void UpdateMovementSpeed(EntityUid uid, MiGoComponent comp)
    {
        if (!TryComp<MovementSpeedModifierComponent>(uid, out var modifComp))
            return;

        var speed = comp.IsPhysicalForm ? comp.MaterialMovementSpeed : comp.UnMaterialMovementSpeed;
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

            if (comp.MaterializationTime == null)
                continue;

            var secondsLeft = Convert.ToInt32((_timing.CurTime - comp.MaterializationTime.Value).TotalSeconds);//calculate time left in seconds

            if (comp.AlertTime == null || comp.AlertTime > secondsLeft)//update alert if buffer has a different value
            {
                comp.AlertTime = secondsLeft;
                _alerts.ShowAlert(uid, comp.AstralAlert);
            }

            if (_timing.CurTime <= comp.MaterializationTime.Value)
                continue;

            ChangeForm(uid, comp, true);
            if (!comp.AudioPlayed)
            {
                _audio.PlayEntity(comp.SoundMaterialize, uid, uid);
                comp.AudioPlayed = true;
            }
            _actions.StartUseDelay(comp.MiGoAstralActionEntity);
        }
    }
    #endregion

    #region Enslave
    private void MiGoEnslave(Entity<MiGoComponent> uid, ref MiGoEnslavementEvent args)
    {
        if (args.Handled)
            return;

        if (!_mind.TryGetMind(args.Target, out var mindId, out var mind))
        {
            //_popup.PopupEntity(Loc.GetString("cult-yogg-no-mind"), args.Target, uid); // commenting cause its spamming sevral times
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

        if (HasComp<BibleUserComponent>(uid))
        {
            _popup.PopupEntity(Loc.GetString("cult-yogg-enslave-cant-be-a-priest"), args.Target, uid);
            return;
        }

        if (HasComp<CultYoggSacrificialComponent>(uid))
        {
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

        _audio.PlayPredicted(uid.Comp.EnslavingSound, args.Target, args.Target);

        args.Handled = true;
    }
    private void MiGoEnslaveOnDoAfter(Entity<MiGoComponent> uid, ref MiGoEnslaveDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null)
            return;

        var ev = new CultYoggEnslavedEvent(args.Target);
        RaiseLocalEvent(uid, ref ev, true);

        _statusEffectsSystem.TryRemoveStatusEffect(args.Target.Value, uid.Comp.RequiedEffect); //Remove Rave cause he already cultist

        args.Handled = true;
    }
    #endregion
}
