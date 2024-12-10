// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Botany;
using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Server.Popups;
using Content.Shared.Botany;
using Content.Shared.Examine;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.SS220.CultYogg.FungusMachine;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.SS220.CultYogg.Fungus;

public sealed class FungusSystem : EntitySystem
{
    [Dependency] private readonly BotanySystem _botany = default!;
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FungusComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<FungusComponent, InteractHandEvent>(OnInteractHand);

        Subs.BuiEvents<FungusMachineComponent>(FungusMachineUiKey.Key,
            subs =>
        {
            subs.Event<FungusSelectedId>(OnUIButton);
        });
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<FungusComponent>();
        while (query.MoveNext(out var uid, out var plantHolder))
        {
            if (plantHolder.NextUpdate > _gameTiming.CurTime)
                continue;
            plantHolder.NextUpdate = _gameTiming.CurTime + plantHolder.UpdateDelay;

            UpdateFungus(uid, plantHolder);
        }
    }

    /// <summary>
    /// Method returning the stage of plant germination within the limits specified in Seed.GrowthStages
    /// </summary>
    /// <param name="entity"></param>
    /// <returns>Current stage number</returns>
    private int GetCurrentGrowthStage(Entity<FungusComponent> entity)
    {
        var (_, component) = entity;

        if (component.Seed == null)
            return 0;

        var result = Math.Max(1, (int) (component.Age * component.Seed.GrowthStages / component.Seed.Maturation));
        return result > component.Seed.GrowthStages ? component.Seed.GrowthStages : result;
    }

    private void OnExamine(Entity<FungusComponent> entity, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        using (args.PushGroup(nameof(FungusComponent)))
        {
            args.PushMarkup(entity.Comp.Seed == null
                ? Loc.GetString("plant-holder-component-nothing-planted-message")
                : Loc.GetString("plant-holder-component-dead-plant-matter-message"));
        }
    }

    private void OnInteractHand(Entity<FungusComponent> entity, ref InteractHandEvent args)
    {
        DoHarvest(entity, args.User, entity.Comp);
    }

    public void UpdateFungus(EntityUid uid, FungusComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var curTime = _gameTiming.CurTime;

        if (curTime < (component.LastCycle + component.CycleDelay)
            || component.Seed == null)
        {
            if (component.UpdateSpriteAfterUpdate)
                UpdateSprite(uid, component);
            return;
        }

        component.LastCycle = curTime;
        component.Age += 1;

        component.UpdateSpriteAfterUpdate = true;
        if (component.Seed.ProductPrototypes.Count > 0)
        {
            if (component.Age > component.Seed.Production)
            {
                if (component.Age - component.LastProduce > component.Seed.Production && !component.HarvestReady)
                {
                    component.HarvestReady = true;
                    component.LastProduce = component.Age;
                }
            }
            else
            {
                if (component.HarvestReady)
                {
                    component.HarvestReady = false;
                    component.LastProduce = component.Age;
                }
            }
        }

        if (component.UpdateSpriteAfterUpdate)
            UpdateSprite(uid, component);
    }

    public bool DoHarvest(EntityUid uid, EntityUid user, FungusComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (component.Seed == null || Deleted(user))
            return false;


        if (!component.HarvestReady)
            return false;

        if (TryComp<HandsComponent>(user, out var hands))
        {
            if (!_botany.CanHarvest(component.Seed, hands.ActiveHandEntity))
            {
                return false;
            }
        }
        else if (!_botany.CanHarvest(component.Seed))
        {
            return false;
        }

        _botany.Harvest(component.Seed, user);
        AfterHarvest(uid, component);
        return true;

    }

    private void AfterHarvest(EntityUid uid, FungusComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.HarvestReady = false;
        component.LastProduce = component.Age;

        if (component.Seed?.HarvestRepeat == HarvestType.NoRepeat)
            RemovePlant(uid, component);
        UpdateSprite(uid, component);
    }

    public void RemovePlant(EntityUid uid, FungusComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.Seed = null;
        component.Age = 0;
        component.LastProduce = 0;
        component.HarvestReady = false;

        UpdateSprite(uid, component);
    }

    private FungusMachineInventoryEntry? GetEntry(EntityUid uid, string entryId, FungusMachineComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return null;

        return component.Inventory.GetValueOrDefault(entryId);
    }

    private void OnUIButton(Entity<FungusMachineComponent> entity, ref FungusSelectedId args)
    {
        var (uid, component) = entity;

        if (args.Actor is not { Valid: true } entit || Deleted(entit))
            return;

        var entry = GetEntry(uid, args.Id, component);

        if (entry == null)
        {
            _popup.PopupEntity(Loc.GetString("vending-machine-component-try-eject-invalid-item"), uid);
            return;
        }

        if (string.IsNullOrEmpty(entry.Id))
            return;

        var proto = _prototype.Index(entry.Id);

        if(TryComp(uid, out FungusComponent? fungusComponent))
        {
            if (proto.TryGetComponent<SeedComponent>("Seed", out var seedComponent))
            {
                if (!_botany.TryGetSeed(seedComponent, out var seed))
                    return;

                _popup.PopupEntity(Loc.GetString("plant-holder-component-plant-success-message",
                        ("seedName", seed.Name),
                        ("seedNoun", seed.Noun)),
                        uid,
                        PopupType.Medium);
                fungusComponent.Seed = seed;
                fungusComponent.Age = 1;
                fungusComponent.LastCycle = _gameTiming.CurTime;
                UpdateSprite(uid, fungusComponent);
            }
        }
    }

    public void UpdateSprite(EntityUid uid, FungusComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.UpdateSpriteAfterUpdate = false;

        if (!TryComp<AppearanceComponent>(uid, out var app))
            return;

        if (component.Seed != null)
        {
            if (component.HarvestReady)
            {
                _appearance.SetData(uid, PlantHolderVisuals.PlantRsi, component.Seed.PlantRsi.ToString(), app);
                _appearance.SetData(uid, PlantHolderVisuals.PlantState, "harvest", app);
            }
            else if (component.Age < component.Seed.Maturation)
            {
                var growthStage = GetCurrentGrowthStage((uid, component));
                _appearance.SetData(uid, PlantHolderVisuals.PlantRsi, component.Seed.PlantRsi.ToString(), app);
                _appearance.SetData(uid, PlantHolderVisuals.PlantState, $"stage-{growthStage}", app);
                component.LastProduce = component.Age;
            }
            else
            {
                _appearance.SetData(uid, PlantHolderVisuals.PlantRsi, component.Seed.PlantRsi.ToString(), app);
                _appearance.SetData(uid, PlantHolderVisuals.PlantState, $"stage-{component.Seed.GrowthStages}", app);
            }
        }
        else
        {
            _appearance.SetData(uid, PlantHolderVisuals.PlantState, "", app);
            _appearance.SetData(uid, PlantHolderVisuals.HealthLight, false, app);
        }
    }
}
