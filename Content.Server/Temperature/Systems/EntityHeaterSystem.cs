using Content.Server.Construction.Components;
using Content.Server.Power.Components;
using Content.Shared.Placeable;
using Content.Shared.Tag;
using Content.Shared.Temperature;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Temperature.Systems;

/// <summary>
/// Handles the server-only parts of <see cref="SharedEntityHeaterSystem"/>
/// </summary>
public sealed class EntityHeaterSystem : SharedEntityHeaterSystem
{
    [Dependency] private readonly TemperatureSystem _temperature = default!;

    //SS220-grill-update begin
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    //SS220-grill-update end

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntityHeaterComponent, MapInitEvent>(OnMapInit);

        //SS220-grill-update begin
        SubscribeLocalEvent<EntityHeaterComponent, ItemRemovedEvent>(OnItemRemovedFromTheGrill);
        SubscribeLocalEvent<EntityHeaterComponent, HeaterSettingChangedEvent>(OnHeaterSettingChanged);
        //SS220-grill-update end
    }

    //SS220-grill-update begin
    private void OnHeaterSettingChanged(Entity<EntityHeaterComponent> ent, ref HeaterSettingChangedEvent args)
    {
        // If the grill has been turned off and there is still food on the grill
        // Stop grilling audio, disable grilling visuals for all food on the grill
        if (args.Setting == EntityHeaterSetting.Off)
        {
            _audio.Stop(ent.Comp.GrillingStream);

            if (TryComp<ItemPlacerComponent>(ent, out var placer))
            {
                foreach (var item in placer.PlacedEntities)
                {
                    _appearance.SetData(item, FoodCookingVisuals.Grilling, false);
                }
            }
        }
    }
    //SS220-grill-update end

    private void OnMapInit(Entity<EntityHeaterComponent> ent, ref MapInitEvent args)
    {
        // Set initial power level
        if (TryComp<ApcPowerReceiverComponent>(ent, out var power))
            power.Load = SettingPower(ent.Comp.Setting, ent.Comp.Power);
    }

    //SS220-grill-update begin
    private void OnItemRemovedFromTheGrill(Entity<EntityHeaterComponent> ent, ref ItemRemovedEvent args)
    {
        // Disable grill smoke visuals
        _appearance.SetData(args.OtherEntity, FoodCookingVisuals.Grilling, false);

        if (TryComp<ItemPlacerComponent>(ent, out var placer))
        {
            // When removing item from the grill, if it is the last one -> stop playing audio
            if (placer.PlacedEntities.Count == 0)
            {
                _audio.Stop(ent.Comp.GrillingStream);
            }
        }
    }
    //SS220-grill-update end

    public override void Update(float deltaTime)
    {
        var query = EntityQueryEnumerator<EntityHeaterComponent, ItemPlacerComponent, ApcPowerReceiverComponent>();
        while (query.MoveNext(out _, out var heater, out var placer, out var power)) //SS220-grill-update. To get to heater component
        {
            if (!power.Powered ||
                heater.Setting == EntityHeaterSetting.Off) //SS220-grill-update. Don't grill, if grill is off
                continue;

            // don't divide by total entities since it's a big grill
            // excess would just be wasted in the air but that's not worth simulating
            // if you want a heater thermomachine just use that...
            var energy = power.PowerReceived * deltaTime;
            foreach (var ent in placer.PlacedEntities)
            {
                _temperature.ChangeHeat(ent, energy);

                //SS220-grill-update begin
                // Skip visuals, if entity can't be cooked on the grill or if entity is already cooked
                if (!HasComp<ConstructionComponent>(ent) || _tagSystem.HasTag(ent, $"Cooked"))
                    continue;

                // If there is no grilling audio playing -> play grilling audio
                if (!_audio.IsPlaying(heater.GrillingStream))
                {
                    heater.GrillingStream = _audio.PlayPvs(heater.GrillSound,
                            ent,
                            AudioParams.Default.WithMaxDistance(10f).WithLoop(true))
                        ?.Entity;
                }

                // Enable grill smoke
                _appearance.SetData(ent, FoodCookingVisuals.Grilling, true);
                //SS220-grill-update end
            }
        }
    }

    /// <remarks>
    /// <see cref="ApcPowerReceiverComponent"/> doesn't exist on the client, so we need
    /// this server-only override to handle setting the network load.
    /// </remarks>
    protected override void ChangeSetting(Entity<EntityHeaterComponent> ent,
        EntityHeaterSetting setting,
        EntityUid? user = null)
    {
        base.ChangeSetting(ent, setting, user);

        if (!TryComp<ApcPowerReceiverComponent>(ent, out var power))
            return;

        power.Load = SettingPower(setting, ent.Comp.Power);

        //SS220-grill-update begin
        var ev = new HeaterSettingChangedEvent(ent, setting);
        RaiseLocalEvent(ent, ref ev);
        //SS220-grill-update end
    }

    //SS220-grill-update
    [ByRefEvent]
    public readonly record struct HeaterSettingChangedEvent(EntityUid HeaterEntity, EntityHeaterSetting Setting);
}
