// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Humanoid;
using Content.Server.SS220.Bed.Cryostorage;
using Content.Server.SS220.GameTicking.Rules;
using Content.Shared.Actions;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Cloning.Events;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Interaction.Events;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.SS220.CultYogg.Cultists;
using Content.Shared.SS220.StuckOnEquip;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Linq;
using Content.Shared.SS220.EntityEffects.Events;
using Content.Shared.Medical;

namespace Content.Server.SS220.CultYogg.Cultists;

public sealed class CultYoggSystem : SharedCultYoggSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly HungerSystem _hungerSystem = default!;
    [Dependency] private readonly SharedStuckOnEquipSystem _stuckOnEquip = default!;
    [Dependency] private readonly ThirstSystem _thirstSystem = default!;
    [Dependency] private readonly VomitSystem _vomitSystem = default!;
    [Dependency] private readonly CultYoggRuleSystem _cultRuleSystem = default!;

    private const string CultDefaultMarking = "CultStage-Halo";

    public override void Initialize()
    {
        base.Initialize();

        // actions
        SubscribeLocalEvent<CultYoggComponent, CultYoggPukeShroomActionEvent>(OnPukeAction);
        SubscribeLocalEvent<CultYoggComponent, CultYoggDigestActionEvent>(OnDigestAction);

        SubscribeLocalEvent<CultYoggComponent, OnSaintWaterDrinkEvent>(OnSaintWaterDrinked);
        SubscribeLocalEvent<CultYoggComponent, ChangeCultYoggStageEvent>(OnUpdateStage);
        SubscribeLocalEvent<CultYoggComponent, CloningEvent>(OnCloning);

        SubscribeLocalEvent<CultYoggComponent, PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<CultYoggComponent, BeingCryoDeletedEvent>(OnCryoDeleted);
        SubscribeLocalEvent<CultYoggComponent, SuicideEvent>(OnSuicide);
    }

    #region StageUpdating
    private void OnUpdateStage(Entity<CultYoggComponent> ent, ref ChangeCultYoggStageEvent args)
    {
        if (ent.Comp.CurrentStage == args.Stage)
            return;

        ent.Comp.CurrentStage = args.Stage;//Upgating stage in component

        UpdateCultVisuals(ent);
        Dirty(ent);
    }

    public void UpdateCultVisuals(Entity<CultYoggComponent> ent)
    {
        if (!TryComp<HumanoidAppearanceComponent>(ent, out var huAp))
            return;

        switch (ent.Comp.CurrentStage)
        {
            case CultYoggStage.Initial:
                break;

            case CultYoggStage.Reveal:
                ent.Comp.PreviousEyeColor = new Color(huAp.EyeColor.R, huAp.EyeColor.G, huAp.EyeColor.B, huAp.EyeColor.A);
                huAp.EyeColor = Color.Green;
                break;

            case CultYoggStage.Alarm:
                if (!_prototype.HasIndex<MarkingPrototype>(CultDefaultMarking))
                {
                    Log.Error($"{CultDefaultMarking} marking doesn't exist");
                    return;
                }

                if (huAp.MarkingSet.Markings.TryGetValue(MarkingCategories.Special, out var value))
                {
                    ent.Comp.PreviousTail = value.FirstOrDefault();
                    value.Clear();
                }

                if (!huAp.MarkingSet.Markings.ContainsKey(MarkingCategories.Special))
                {
                    huAp.MarkingSet.Markings.Add(MarkingCategories.Special, new List<Marking>([new Marking(CultDefaultMarking, colorCount: 1)]));
                }

                _humanoidAppearance.SetMarkingId(ent.Owner,
                    MarkingCategories.Special,
                    0,
                    CultDefaultMarking,
                    huAp);

                var newMarkingId = $"CultStage-{huAp.Species}";

                if (!_prototype.HasIndex<MarkingPrototype>(newMarkingId))
                {
                    // We have species-marking only for the Nians, so this log only leads to unnecessary errors.
                    //Log.Error($"{newMarkingId} marking doesn't exist");
                    return;
                }

                huAp.MarkingSet.Markings[MarkingCategories.Special].Add(new Marking(newMarkingId, colorCount: 1));
                break;

            case CultYoggStage.God:
                if (!TryComp<MobStateComponent>(ent, out var mobstate))
                    return;

                if (mobstate.CurrentState == MobState.Dead) //if cultists is dead we skip this one
                    return;

                AcsendCultist(ent);
                break;

            default:
                Log.Error("Something went wrong with CultYogg stages");
                break;
        }
    }

    public void DeleteVisuals(Entity<CultYoggComponent> ent)
    {
        if (!TryComp<HumanoidAppearanceComponent>(ent, out var huAp))
            return;

        if (ent.Comp.PreviousEyeColor != null)
            huAp.EyeColor = ent.Comp.PreviousEyeColor.Value;

        huAp.MarkingSet.Markings.Remove(MarkingCategories.Special);

        if (huAp.MarkingSet.Markings.ContainsKey(MarkingCategories.Tail) &&
            ent.Comp.PreviousTail != null)
        {
            huAp.MarkingSet.Markings[MarkingCategories.Tail].Add(ent.Comp.PreviousTail);
        }

        Dirty(ent.Owner, huAp);
    }
    #endregion

    #region Puke
    private void OnPukeAction(Entity<CultYoggComponent> ent, ref CultYoggPukeShroomActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        _vomitSystem.Vomit(ent);
        _entityManager.SpawnEntity(ent.Comp.PukedEntity, Transform(ent).Coordinates);

        _actions.RemoveAction(ent.Owner, ent.Comp.PukeShroomActionEntity);
        _actions.AddAction(ent, ref ent.Comp.DigestActionEntity, ent.Comp.DigestAction);
    }

    private void OnDigestAction(Entity<CultYoggComponent> ent, ref CultYoggDigestActionEvent args)
    {
        if (!TryComp<HungerComponent>(ent, out var hungerComp))
            return;

        if (!TryComp<ThirstComponent>(ent, out var thirstComp))
            return;

        var currentHunger = _hungerSystem.GetHunger(hungerComp);
        if (currentHunger <= ent.Comp.HungerCost || hungerComp.CurrentThreshold == ent.Comp.MinHungerThreshold)
        {
            _popup.PopupEntity(Loc.GetString("cult-yogg-digest-no-nutritions"), ent);
            //_popup.PopupClient(Loc.GetString("cult-yogg-digest-no-nutritions"), ent, ent);//idk if it isn't working, but OnSericultureStart is an ok
            return;
        }

        if (thirstComp.CurrentThirst <= ent.Comp.ThirstCost || thirstComp.CurrentThirstThreshold == ent.Comp.MinThirstThreshold)
        {
            _popup.PopupEntity(Loc.GetString("cult-yogg-digest-no-water"), ent);
            return;
        }

        _hungerSystem.ModifyHunger(ent, -ent.Comp.HungerCost);

        _thirstSystem.ModifyThirst(ent, thirstComp, -ent.Comp.ThirstCost);

        _actions.RemoveAction(ent.Owner, ent.Comp.DigestActionEntity);//if we digested, we should puke after

        if (_actions.AddAction(ent, ref ent.Comp.PukeShroomActionEntity, out var act, ent.Comp.PukeShroomAction) && act.UseDelay != null) //useDelay when added
        {
            var start = _timing.CurTime;
            var end = start + act.UseDelay.Value;
            _actions.SetCooldown(ent.Comp.PukeShroomActionEntity.Value, start, end);
        }
    }
    #endregion

    #region Ascending
    public void AcsendCultist(Entity<CultYoggComponent> ent)
    {
        if (TerminatingOrDeleted(ent))
            return;

        // Get original body position and spawn MiGo here
        var migo = _entityManager.SpawnAtPosition(ent.Comp.AscendedEntity, Transform(ent).Coordinates);


        if (_mind.TryGetMind(ent, out var mindId, out var mind))
            _mind.TransferTo(mindId, migo, mind: mind);// Move the mind if there is one and it's supposed to be transferred

        //Gib original body
        if (TryComp<BodyComponent>(ent, out var body))
            _body.GibBody(ent, body: body);
    }

    public bool TryStartAscensionByReagent(EntityUid ent, CultYoggComponent comp)
    {
        if (comp.ConsumedAscensionReagent < comp.AmountAscensionReagentAscend)
            return false;

        StartAscension(ent);
        return true;
    }

    public void StartAscension(EntityUid ent)
    { //idk if it is canser or no, will be like that for a time
        if (HasComp<AcsendingComponent>(ent))
            return;

        if (!NoAcsendingCultists())//to prevent becaming MiGo at the same time
        {
            _popup.PopupEntity(Loc.GetString("cult-yogg-acsending-have-acsending"), ent, ent);
            return;
        }
        _popup.PopupEntity(Loc.GetString("cult-yogg-acsending-started"), ent, ent);
        EnsureComp<AcsendingComponent>(ent);
    }

    public void ResetCultist(Entity<CultYoggComponent> ent)//idk if it is canser or no, will be like that for a time
    {
        if (RemComp<AcsendingComponent>(ent))
            _popup.PopupEntity(Loc.GetString("cult-yogg-acsending-stopped"), ent, ent);

        ent.Comp.ConsumedAscensionReagent = 0;

        if (_stuckOnEquip.TryRemoveStuckItems(ent))//Idk how to deal with popup spamming
            _popup.PopupEntity(Loc.GetString("cult-yogg-dropped-items"), ent, ent);//and now i dont see any :(

        Dirty(ent, ent.Comp);
    }

    private bool NoAcsendingCultists()//if anybody else is acsending
    {
        var query = EntityQueryEnumerator<AcsendingComponent>();
        while (query.MoveNext(out _, out _))
        {
            return false;
        }
        return true;
    }
    #endregion

    #region Purifying
    private void OnSaintWaterDrinked(Entity<CultYoggComponent> ent, ref OnSaintWaterDrinkEvent args)
    {
        EnsureComp<CultYoggPurifiedComponent>(ent, out var purifyedComp);
        purifyedComp.TotalAmountOfHolyWater += args.SaintWaterAmount;

        if (purifyedComp.TotalAmountOfHolyWater >= purifyedComp.AmountToPurify)
        {
            //After purifying effect
            _audio.PlayPvs(purifyedComp.PurifyingCollection, ent);

            DeleteVisuals(ent);

            RemComp<CultYoggComponent>(ent);
            _cultRuleSystem.CheckSimplifiedEslavement();//Add token if it was last cultist
        }

        purifyedComp.PurifyingDecayEventTime = _timing.CurTime + purifyedComp.BeforeDeclinesTime; //setting timer, when purifying will be removed
        Dirty(ent, ent.Comp);
    }
    #endregion

    #region CheckSimplifiedEslavement
    private void OnPlayerDetached(Entity<CultYoggComponent> ent, ref PlayerDetachedEvent args)
    {
        _cultRuleSystem.CheckSimplifiedEslavement();
    }

    private void OnCryoDeleted(Entity<CultYoggComponent> ent, ref BeingCryoDeletedEvent args)
    {
        _cultRuleSystem.CheckSimplifiedEslavement();
    }

    private void OnSuicide(Entity<CultYoggComponent> ent, ref SuicideEvent args)
    {
        _cultRuleSystem.CheckSimplifiedEslavement();
    }
    #endregion

    private void OnCloning(Entity<CultYoggComponent> ent, ref CloningEvent args)//ToDo_SS220 somthing wierd happned when we are cloning with cult markings
    {
        if (!_cultRuleSystem.TryGetCultGameRule(out var rule))
            return;

        _cultRuleSystem.MakeCultist(args.CloneUid, rule.Value);
    }
}
