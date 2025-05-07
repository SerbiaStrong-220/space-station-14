// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.Alert;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Eye;
using Content.Shared.FixedPoint;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.SS220.CultYogg.MiGo;
using Content.Shared.StatusEffect;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Content.Shared.Projectiles;
using Content.Server.Projectiles;


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
    [Dependency] private readonly StomachSystem _stomach = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly ProjectileSystem _projectile = default!;

    private const string AscensionReagent = "TheBloodOfYogg";

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
            _tag.RemoveTag(uid, "MiGoInAstral");
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
            _tag.AddTag(uid, "MiGoInAstral");
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

            if (TryComp<EmbeddedContainerComponent>(uid, out var embeddedContainer))
                _projectile.DetachAllEmbedded((uid, embeddedContainer));

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

        // Remove ascension reagent
        if (_body.TryGetBodyOrganEntityComps<StomachComponent>(args.Target.Value, out var stomachs))
        {
            foreach (var stomach in stomachs)
            {
                if (stomach.Comp2.Body is not { } body)
                    continue;

                var reagentRoRemove = new ReagentQuantity(AscensionReagent, FixedPoint2.MaxValue);
                _stomach.TryRemoveReagent(stomach, reagentRoRemove); // Removes from stomach

                if (_solutionContainer.TryGetSolution(body, stomach.Comp1.BodySolutionName, out var bodySolutionEnt, out var bodySolution) &&
                    bodySolution != null)
                {
                    bodySolution.RemoveReagent(reagentRoRemove); // Removes from body
                    _solutionContainer.UpdateChemicals(bodySolutionEnt.Value);
                }
            }
        }

        args.Handled = true;
    }
    #endregion
}
