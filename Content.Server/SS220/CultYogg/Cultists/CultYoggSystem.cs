// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Humanoid;
using Content.Server.Medical;
using Content.Server.SS220.GameTicking.Rules;
using Content.Shared.Actions;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Mind;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.SS220.CultYogg.Cultists;
using Content.Shared.SS220.CultYogg.MiGo;
using Content.Shared.SS220.StuckOnEquip;
using Robust.Shared.Timing;
using Robust.Shared.Prototypes;
using System.Linq;
using Robust.Shared.Audio.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Robust.Shared.Network;
using Content.Shared.SS220.Roles;
using Content.Shared.SS220.EntityEffects;

namespace Content.Server.SS220.CultYogg.Cultists;

public sealed class CultYoggSystem : SharedCultYoggSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly CultYoggRuleSystem _cultRule = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly HungerSystem _hungerSystem = default!;
    [Dependency] private readonly ThirstSystem _thirstSystem = default!;
    [Dependency] private readonly VomitSystem _vomitSystem = default!;

    private const string CultDefaultMarking = "CultStage-Halo";

    public override void Initialize()
    {
        base.Initialize();

        // actions
        SubscribeLocalEvent<CultYoggComponent, CultYoggPukeShroomEvent>(PukeAction);
        SubscribeLocalEvent<CultYoggComponent, CultYoggDigestEvent>(DigestAction);
        SubscribeLocalEvent<CultYoggComponent, CultYoggAscendingEvent>(AscendingAction);

        SubscribeLocalEvent<CultYoggComponent, OnSaintWaterDrinkEvent>(OnSaintWaterDrinked);
        SubscribeLocalEvent<CultYoggComponent, CultYoggForceAscendingEvent>(ForcedAcsending);
        SubscribeLocalEvent<CultYoggComponent, ChangeCultYoggStageEvent>(UpdateStage);
    }

    #region StageUpdating
    private void UpdateStage(Entity<CultYoggComponent> ent, ref ChangeCultYoggStageEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        ApplyCultVisuals(ent, args.Stage);
    }

    public void ApplyCultVisuals(Entity<CultYoggComponent> ent, CultYoggStage stage)
    {
        if (!TryComp<HumanoidAppearanceComponent>(ent, out var huAp))
            return;

        if (ent.Comp.CurrentStage == stage)
            return;

        ent.Comp.CurrentStage = stage;//Upgating stage in component

        switch (stage)
        {
            case CultYoggStage.Initial:
                return;
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

                if (mobstate.CurrentState != MobState.Dead) //if he is dead we skip him
                {
                    var ev = new CultYoggForceAscendingEvent();//making cultist MiGo
                    RaiseLocalEvent(ent, ref ev);
                }
                break;
            default:
                Log.Error("Something went wrong with CultYogg stages");
                break;
        }
        Dirty(ent.Owner, huAp);
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
    private void PukeAction(Entity<CultYoggComponent> ent, ref CultYoggPukeShroomEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        _vomitSystem.Vomit(ent);
        var shroom = _entityManager.SpawnEntity(ent.Comp.PukedEntity, Transform(ent).Coordinates);

        _actions.RemoveAction(ent.Owner, ent.Comp.PukeShroomActionEntity);
        _actions.AddAction(ent, ref ent.Comp.DigestActionEntity, ent.Comp.DigestAction);
    }

    private void DigestAction(Entity<CultYoggComponent> ent, ref CultYoggDigestEvent args)
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
    private void AscendingAction(Entity<CultYoggComponent> ent, ref CultYoggAscendingEvent args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        if (HasComp<AcsendingComponent>(ent))
            return;

        // Get original body position and spawn MiGo here
        var migo = _entityManager.SpawnAtPosition(ent.Comp.AscendedEntity, Transform(ent).Coordinates);

        // Move the mind if there is one and it's supposed to be transferred
        if (_mind.TryGetMind(ent, out var mindId, out var mind))
            _mind.TransferTo(mindId, migo, mind: mind);

        //Gib original body
        if (TryComp<BodyComponent>(ent, out var body))
            _body.GibBody(ent, body: body);
    }

    private void ForcedAcsending(Entity<CultYoggComponent> ent, ref CultYoggForceAscendingEvent args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        // Get original body position and spawn MiGo here
        var migo = _entityManager.SpawnAtPosition(ent.Comp.AscendedEntity, Transform(ent).Coordinates);

        // Move the mind if there is one and it's supposed to be transferred
        if (_mind.TryGetMind(ent, out var mindId, out var mind))
            _mind.TransferTo(mindId, migo, mind: mind);

        //Gib original body
        if (TryComp<BodyComponent>(ent, out var body))
            _body.GibBody(ent, body: body);
    }

    public bool TryStartAscensionByReagent(EntityUid ent, CultYoggComponent comp)
    {
        if (comp.ConsumedAscensionReagent < comp.AmountAscensionReagentAscend)
            return false;

        StartAscension(ent, comp);
        return true;
    }

    public void StartAscension(EntityUid ent, CultYoggComponent comp)
    { //idk if it is canser or no, will be like that for a time
        if (HasComp<AcsendingComponent>(ent))
            return;

        if (!AcsendingCultistCheck())//to prevent becaming MiGo at the same time
        {
            _popup.PopupEntity(Loc.GetString("cult-yogg-acsending-have-acsending"), ent, ent);
            return;
        }
        _popup.PopupEntity(Loc.GetString("cult-yogg-acsending-started"), ent, ent);
        EnsureComp<AcsendingComponent>(ent);
    }

    public void NullifyShroomEffect(EntityUid ent, CultYoggComponent comp)//idk if it is canser or no, will be like that for a time
    {
        string? message = null;
        if (RemComp<AcsendingComponent>(ent) ||
            comp.ConsumedAscensionReagent > 0)
            message += Loc.GetString("cult-yogg-acsending-stopped");

        comp.ConsumedAscensionReagent = 0;

        //Remove all corrupted items
        var ev = new DropAllStuckOnEquipEvent(ent);
        RaiseLocalEvent(ent, ref ev, true);

        if (ev.DroppedItems.Count > 0)
            message += message is null
                ? Loc.GetString("cult-yogg-dropped-items")
                : " " + Loc.GetString("cult-yogg-dropped-items-not-first");

        _popup.PopupEntity(message, ent, ent);
    }

    private bool AcsendingCultistCheck()//if anybody else is acsending
    {
        var query = EntityQueryEnumerator<CultYoggComponent, AcsendingComponent>();
        while (query.MoveNext(out var ent, out _, out _))
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
        }

        purifyedComp.PurifyingDecayEventTime = _timing.CurTime + purifyedComp.BeforeDeclinesTime; //setting timer, when purifying will be removed
    }
    #endregion
}
