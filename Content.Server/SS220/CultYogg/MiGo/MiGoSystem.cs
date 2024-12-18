// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Alert;
using Content.Shared.Eye;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.SS220.CultYogg.MiGo;
using Content.Shared.StatusEffect;
using Content.Shared.Tag;
using Robust.Server.GameObjects;


namespace Content.Server.SS220.CultYogg.MiGo;

public sealed partial class MiGoSystem : SharedMiGoSystem
{
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;
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
