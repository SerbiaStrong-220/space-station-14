// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Bible.Components;
using Content.Shared.Alert;
using Content.Shared.DoAfter;
using Content.Shared.Eye;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mindshield.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Popups;
using Content.Shared.Revolutionary.Components;
using Content.Shared.SS220.CultYogg.MiGo;
using Content.Shared.SS220.CultYogg.Sacraficials;
using Content.Shared.StatusEffect;
using Content.Shared.Tag;
using Content.Shared.Zombies;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;


namespace Content.Server.SS220.CultYogg.MiGo;

public sealed partial class MiGoSystem : SharedMiGoSystem
{

    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifier = default!;
    [Dependency] private readonly VisibilitySystem _visibility = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    public override void Initialize()
    {
        base.Initialize();

        //actions
        SubscribeLocalEvent<MiGoComponent, MiGoEnslavementEvent>(MiGoEnslave);

        SubscribeLocalEvent<MiGoComponent, MiGoEnslaveDoAfterEvent>(MiGoEnslaveOnDoAfter);
    }

    #region Astral
    public override void ChangeForm(EntityUid uid, MiGoComponent comp, bool isMaterial)
    {
        comp.IsPhysicalForm = isMaterial;

        base.ChangeForm(uid, comp, isMaterial);

        if (!TryComp<VisibilityComponent>(uid, out var vis))
            return;

        if (isMaterial)
        {
            //no opening door during astral
            _tag.AddTag(uid, "DoorBumpOpener");
            comp.MaterializationTime = null;
            comp.AlertTime = 0;

            _alerts.ClearAlert(uid, comp.AstralAlert);

            RemComp<MovementIgnoreGravityComponent>(uid);

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

            //no phisyc during astral
            EnsureComp<MovementIgnoreGravityComponent>(uid);

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

    #endregion

    #region Enslave
    private void MiGoEnslave(Entity<MiGoComponent> uid, ref MiGoEnslavementEvent args)
    {
        if (args.Handled)
            return;

        if (!uid.Comp.IsPhysicalForm)
            return;

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

        if (!_mind.TryGetMind(args.Target, out var mindId, out var mind))//Its here bea
        {
            //_popup.PopupEntity(Loc.GetString("cult-yogg-no-mind"), args.Target, uid); // commenting cause its spamming sevral times
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
