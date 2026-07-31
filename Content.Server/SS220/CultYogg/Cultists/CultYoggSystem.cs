// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Chat.Managers;
using Content.Server.SS220.Bed.Cryostorage;
using Content.Server.SS220.GameTicking.Rules;
using Content.Shared.Actions;
using Content.Shared.Body;
using Content.Shared.Cloning.Events;
using Content.Shared.Gibbing;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.SS220.CultYogg.Cultists;
using Content.Shared.SS220.EntityEffects.Events;
using Content.Shared.SS220.StuckOnEquip;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.SS220.CultYogg.Cultists;

public sealed partial class CultYoggSystem : SharedCultYoggSystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private GibbingSystem _gibbing = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private HungerSystem _hungerSystem = default!;
    [Dependency] private SharedStuckOnEquipSystem _stuckOnEquip = default!;
    [Dependency] private ThirstSystem _thirstSystem = default!;
    [Dependency] private CultYoggRuleSystem _cultRuleSystem = default!;
    [Dependency] private IChatManager _chatManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        DebugTools.Assert(CultDefaultMarking.Id.Contains(CultMarkingCommonPart));

        // actions
        SubscribeLocalEvent<CultYoggComponent, CultYoggPukeShroomActionEvent>(OnPukeAction);
        SubscribeLocalEvent<CultYoggComponent, CultYoggDigestActionEvent>(OnDigestAction);

        SubscribeLocalEvent<CultYoggComponent, OnSaintWaterDrinkEvent>(OnSaintWaterDrinked);
        SubscribeLocalEvent<CultYoggComponent, ChangeCultYoggStageEvent>(OnUpdateStage);
        SubscribeLocalEvent<CultYoggComponent, CloningEvent>(OnCloning);

        SubscribeLocalEvent<CultYoggComponent, BeingCryoDeletedEvent>(OnCryoDeleted);
    }

    #region Visuals
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

        switch (ent.Comp.CurrentStage)
        {
            case CultYoggStage.Initial:
                break;

            case CultYoggStage.Reveal:
                EnsureEyesColor(ent);
                break;

            case CultYoggStage.Alarm:
                EnsureEyesColor(ent);
                EnsureHalo(ent);
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

    public override void DeleteVisuals(Entity<CultYoggComponent> ent)
    {
        _visualBody.TryGatherMarkingsData(ent.Owner, [HumanoidVisualLayers.Eyes], out var eyesProfiles, out _, out _);
        _visualBody.TryGatherMarkingsData(ent.Owner, [HumanoidVisualLayers.Special], out _, out _, out var appliedMarkings);
        _visualBody.TryGatherMarkingsData(ent.Owner, [HumanoidVisualLayers.Tail], out _, out _, out var appliedTailMarkings);

        if (ent.Comp.PreviousEyeColor is not null && eyesProfiles is not null
            && eyesProfiles.TryGetValue(EyesCategory, out var eyesProfile))
        {
            eyesProfile.EyeColor = ent.Comp.PreviousEyeColor.Value;
            _visualBody.ApplyProfile(ent, eyesProfile);
            ent.Comp.PreviousEyeColor = null;
        }

        if (appliedMarkings is not null && appliedMarkings.TryGetValue(TorsoCategory, out var torsoSpecialMarkings)
            && torsoSpecialMarkings.TryGetValue(HumanoidVisualLayers.Special, out var specialMarkingsList))
        {
            specialMarkingsList.RemoveAll(x => x.MarkingId.Id.Contains(CultMarkingCommonPart));
            _visualBody.ApplyMarkings(ent, new() { { TorsoCategory, torsoSpecialMarkings } });
        }

        if (ent.Comp.PreviousTailMarkings is { } previousTailMarkings
            && appliedTailMarkings is not null && appliedTailMarkings.TryGetValue(TorsoCategory, out var torsoTailMarkings)
            && torsoTailMarkings.TryGetValue(HumanoidVisualLayers.Tail, out var tailMarkingsList))
        {
            _visualBody.ApplyMarkings(ent, new() { { TorsoCategory, new() { { HumanoidVisualLayers.Tail, previousTailMarkings } } } });
            ent.Comp.PreviousTailMarkings = null;
        }
    }
    #endregion

    #region Puke
    private void OnPukeAction(Entity<CultYoggComponent> ent, ref CultYoggPukeShroomActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        _audio.PlayPvs(ent.Comp.VomitSound, ent);

        Spawn(ent.Comp.PukedEntity, Transform(ent).Coordinates);

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
            _popup.PopupClient(Loc.GetString("cult-yogg-digest-no-nutritions"), ent, ent);
            return;
        }

        if (thirstComp.CurrentThirst <= ent.Comp.ThirstCost || thirstComp.CurrentThirstThreshold == ent.Comp.MinThirstThreshold)
        {
            _popup.PopupClient(Loc.GetString("cult-yogg-digest-no-water"), ent, ent);
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
        var migo = SpawnAtPosition(ent.Comp.AscendedEntity, Transform(ent).Coordinates);

        if (_mind.TryGetMind(ent, out var mindId, out var mind))
            _mind.TransferTo(mindId, migo, mind: mind);// Move the mind if there is one and it's supposed to be transferred

        //Gib original body
        if (TryComp<BodyComponent>(ent, out var body))
            _gibbing.Gib(ent);
    }

    public bool TryStartAscensionByReagent(EntityUid ent, CultYoggComponent comp)
    {
        if (comp.ConsumedAscensionReagent < comp.AmountAscensionReagentAscend)
            return false;

        StartAscension(ent);
        return true;
    }

    public void StartAscension(EntityUid ent)
    {
        //idk if it is canser or no, will be like that for a time
        if (HasComp<AcsendingComponent>(ent))
            return;

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
    #endregion

    #region Purifying
    private void OnSaintWaterDrinked(Entity<CultYoggComponent> ent, ref OnSaintWaterDrinkEvent args)
    {
        EnsureComp<CultYoggPurifiedComponent>(ent, out var purifyedComp);
        purifyedComp.TotalAmountOfHolyWater += args.SaintWaterAmount;

        if (purifyedComp.TotalAmountOfHolyWater >= purifyedComp.AmountToPurify)
            purifyedComp.PurifyTime ??= _timing.CurTime + purifyedComp.BeforePurifyingTime;

        var liberationEvent = new LiberationFromCultEvent();
        RaiseLocalEvent(ent, ref liberationEvent);

        purifyedComp.DecayTime = _timing.CurTime + purifyedComp.BeforeDecayTime; //setting timer, when purifying will be removed
        Dirty(ent, ent.Comp);
    }
    #endregion

    private void OnCloning(Entity<CultYoggComponent> ent, ref CloningEvent args)//ToDo_SS220 somthing wierd happned when we are cloning with cult markings
    {
        if (!_cultRuleSystem.TryGetCultGameRule(out var rule))
            return;

        _cultRuleSystem.MakeCultist(args.CloneUid, rule.Value);
    }

    private void OnCryoDeleted(Entity<CultYoggComponent> ent, ref BeingCryoDeletedEvent args)
    {
        _chatManager.SendAdminAlert(Loc.GetString("cult-yogg-cultist-deleted-by-cryo", ("ent", ent)));

        var ev = new CultYoggDeCultingEvent(ent);
        RaiseLocalEvent(ent, ref ev, true);
    }
}
