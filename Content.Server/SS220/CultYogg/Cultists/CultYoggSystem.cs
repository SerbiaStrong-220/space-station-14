// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Humanoid;
using Content.Server.Medical;
using Content.Server.SS220.DarkForces.Saint.Reagent.Events;
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
using Robust.Shared.Timing;
using Robust.Shared.Prototypes;
using System.Linq;


namespace Content.Server.SS220.CultYogg.Cultists;

public sealed class CultYoggSystem : SharedCultYoggSystem
{

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
    private void UpdateStage(Entity<CultYoggComponent> entity, ref ChangeCultYoggStageEvent args)
    {
        if (!TryComp<HumanoidAppearanceComponent>(entity, out var huAp))
            return;

        entity.Comp.CurrentStage = args.Stage;//Upgating stage in component

        switch (args.Stage)
        {
            case 0:
                return;
            case 1:
                entity.Comp.PreviousEyeColor = new Color(huAp.EyeColor.R, huAp.EyeColor.G, huAp.EyeColor.B, huAp.EyeColor.A);
                huAp.EyeColor = Color.Green;
                break;
            case 2:
                if (!_prototype.HasIndex<MarkingPrototype>("CultStage-Halo"))
                {
                    Log.Error("CultStage-Halo marking doesn't exist");
                    return;
                }

                if (!huAp.MarkingSet.Markings.ContainsKey(MarkingCategories.Special))
                {
                    huAp.MarkingSet.Markings.Add(MarkingCategories.Special, new List<Marking>([new Marking("CultStage-Halo", colorCount: 1)]));
                    Dirty(entity.Owner, huAp);
                }
                else
                {
                    _humanoidAppearance.SetMarkingId(entity.Owner,
                        MarkingCategories.Special,
                        0,
                        "CultStage-Halo",
                        huAp);
                }

                var newMarkingId = $"CultStage-{huAp.Species}";

                if (!_prototype.HasIndex<MarkingPrototype>(newMarkingId))
                {
                    Log.Error($"{newMarkingId} marking doesn't exist");
                    return;
                }

                if (!huAp.MarkingSet.Markings.ContainsKey(MarkingCategories.Tail) &&
                    newMarkingId != "CultStage-Halo")
                {
                    if (huAp.MarkingSet.Markings[MarkingCategories.Tail].FirstOrDefault() != null)
                        entity.Comp.PreviousTail = huAp.MarkingSet.Markings[MarkingCategories.Tail].FirstOrDefault();
                    _humanoidAppearance.SetMarkingId(entity.Owner,
                        MarkingCategories.Tail,
                        0,
                        newMarkingId,
                        huAp);
                }
                break;
            case 3:
                var ev = new CultYoggForceAscendingEvent();//making cultist MiGo
                RaiseLocalEvent(entity, ref ev);

                break;
            default:
                Log.Error("Something went wrong with CultYogg stages");
                break;
        }
        Dirty(entity.Owner, huAp);
    }

    private void DeleteVisual(Entity<CultYoggComponent> entity)
    {
        if (!TryComp<HumanoidAppearanceComponent>(entity, out var huAp))
            return;

        if (entity.Comp.PreviousEyeColor != null)
            huAp.EyeColor = entity.Comp.PreviousEyeColor.Value;

        if (huAp.MarkingSet.Markings.ContainsKey(MarkingCategories.Special))
        {
            huAp.MarkingSet.Markings.Remove(MarkingCategories.Special);
        }

        if (!huAp.MarkingSet.Markings.ContainsKey(MarkingCategories.Tail) &&
            entity.Comp.PreviousTail != null)
        {
            _humanoidAppearance.SetMarkingId(entity.Owner,
                MarkingCategories.Tail,
                0,
                entity.Comp.PreviousTail.MarkingId,
                huAp);
        }
        Dirty(entity.Owner, huAp);
    }
    #endregion

    #region Puke
    private void PukeAction(Entity<CultYoggComponent> uid, ref CultYoggPukeShroomEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        _vomitSystem.Vomit(uid);
        var shroom = _entityManager.SpawnEntity(uid.Comp.PukedEntity, Transform(uid).Coordinates);

        _actions.RemoveAction(uid, uid.Comp.PukeShroomActionEntity);
        _actions.AddAction(uid, ref uid.Comp.DigestActionEntity, uid.Comp.DigestAction);
    }
    private void DigestAction(Entity<CultYoggComponent> uid, ref CultYoggDigestEvent args)
    {
        if (!TryComp<HungerComponent>(uid, out var hungerComp))
            return;

        if (!TryComp<ThirstComponent>(uid, out var thirstComp))
            return;

        if (hungerComp.CurrentHunger <= uid.Comp.HungerCost || hungerComp.CurrentThreshold == uid.Comp.MinHungerThreshold)
        {
            _popup.PopupEntity(Loc.GetString("cult-yogg-digest-no-nutritions"), uid);
            //_popup.PopupClient(Loc.GetString("cult-yogg-digest-no-nutritions"), uid, uid);//idk if it isn't working, but OnSericultureStart is an ok
            return;
        }

        if (thirstComp.CurrentThirst <= uid.Comp.ThirstCost || thirstComp.CurrentThirstThreshold == uid.Comp.MinThirstThreshold)
        {
            _popup.PopupEntity(Loc.GetString("cult-yogg-digest-no-water"), uid);
            return;
        }

        _hungerSystem.ModifyHunger(uid, -uid.Comp.HungerCost);

        _thirstSystem.ModifyThirst(uid, thirstComp, -uid.Comp.ThirstCost);

        _actions.RemoveAction(uid, uid.Comp.DigestActionEntity);//if we digested, we should puke after

        if (_actions.AddAction(uid, ref uid.Comp.PukeShroomActionEntity, out var act, uid.Comp.PukeShroomAction) && act.UseDelay != null) //useDelay when added
        {
            var start = _timing.CurTime;
            var end = start + act.UseDelay.Value;
            _actions.SetCooldown(uid.Comp.PukeShroomActionEntity.Value, start, end);
        }
    }
    #endregion

    #region Ascending
    private void AscendingAction(Entity<CultYoggComponent> uid, ref CultYoggAscendingEvent args)
    {
        if (TerminatingOrDeleted(uid))
            return;

        // Get original body position and spawn MiGo here
        var migo = _entityManager.SpawnAtPosition(uid.Comp.AscendedEntity, Transform(uid).Coordinates);

        // Move the mind if there is one and it's supposed to be transferred
        if (_mind.TryGetMind(uid, out var mindId, out var mind))
            _mind.TransferTo(mindId, migo, mind: mind);

        //Gib original body
        if (TryComp<BodyComponent>(uid, out var body))
            _body.GibBody(uid, body: body);
    }
    private void ForcedAcsending(Entity<CultYoggComponent> uid, ref CultYoggForceAscendingEvent args)
    {

        if (TerminatingOrDeleted(uid))
            return;

        // Get original body position and spawn MiGo here
        var migo = _entityManager.SpawnAtPosition(uid.Comp.AscendedEntity, Transform(uid).Coordinates);

        // Move the mind if there is one and it's supposed to be transferred
        if (_mind.TryGetMind(uid, out var mindId, out var mind))
            _mind.TransferTo(mindId, migo, mind: mind);

        //Gib original body
        if (TryComp<BodyComponent>(uid, out var body))
            _body.GibBody(uid, body: body);
    }
    public void ModifyEatenShrooms(EntityUid uid, CultYoggComponent comp)//idk if it is canser or no, will be like that for a time
    {
        comp.ConsumedShrooms++; //Add shroom to buffer
        if (comp.ConsumedShrooms <= comp.AmountShroomsToAscend) // if its not enough to ascend
            return;

        if (!AcsendingCultistCheck())//to prevent becaming MiGo at the same time
        {
            _popup.PopupEntity(Loc.GetString("cult-yogg-acsending-have-acsending"), uid, uid);
            return;
        }

        if (!AvaliableMiGoCheck() && !TryReplaceMiGo())//if amount of migo < required amount of migo or have 1 to replace
        {
            _popup.PopupEntity(Loc.GetString("cult-yogg-acsending-migo-full"), uid, uid);
            return;
        }

        //Maybe in later version we will detiriorate the body and add some kind of effects

        //IDK how to check if he already has this action, so i did this markup
        EnsureComp<AcsendingComponent>(uid);
    }

    //Check for avaliable amoiunt of MiGo or gib MiGo to replace
    private bool AvaliableMiGoCheck()
    {
        //Check number of MiGo in gamerule
        _cultRule.GetCultGameRule(out var ruleComp);

        if (ruleComp is null)
            return false;

        int reqMiGo = ruleComp.ReqAmountOfMiGo;

        var query = EntityQueryEnumerator<MiGoComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            reqMiGo--;
        }

        if (reqMiGo > 0)
            return true;

        return false;
    }
    private bool TryReplaceMiGo()
    {
        //if any MiGo needs to be replaced add here
        List<EntityUid> migoOnDelete = new();

        var query = EntityQueryEnumerator<MiGoComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.MayBeReplaced)
                migoOnDelete.Add(uid);
        }

        if ((migoOnDelete.Count != 0) && TryComp<BodyComponent>(migoOnDelete[0], out var body)) //ToDo check for cancer coding
        {
            _body.GibBody(migoOnDelete[0], body: body);
            return true;
        }

        return false;
    }
    private bool AcsendingCultistCheck()//if anybody else is acsending
    {
        var query = EntityQueryEnumerator<CultYoggComponent, AcsendingComponent>();
        while (query.MoveNext(out var uid, out var comp, out var acsComp))
        {
            return false;
        }
        return true;
    }
    #endregion
    private void OnSaintWaterDrinked(Entity<CultYoggComponent> uid, ref OnSaintWaterDrinkEvent args)
    {
        EnsureComp<CultYoggCleansedComponent>(uid, out var cleansedComp);
        cleansedComp.AmountOfHolyWater += args.SaintWaterAmount;

        if (cleansedComp.AmountOfHolyWater >= cleansedComp.AmountToCleance)
            RemComp<CultYoggComponent>(uid);

        cleansedComp.CleansingDecayEventTime = _timing.CurTime + cleansedComp.BeforeDeclinesTime; //setting timer, when cleansing will be removed
    }
}
[ByRefEvent, Serializable]
public record struct CultYoggForceAscendingEvent;
