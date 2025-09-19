using System.Linq;
using Content.Server.Power.Components;
using Content.Shared.Placeable;
using Content.Shared.SS220.EntityEffects.Effects;
using Content.Shared.Tag;
using Content.Shared.Temperature;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.Systems;
using Content.Shared.Whitelist;
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
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    //SS220-grill-update end

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntityHeaterComponent, MapInitEvent>(OnMapInit);

        //SS220-grill-update begin
        SubscribeLocalEvent<EntityHeaterComponent, ItemRemovedEvent>(OnItemRemoved);
        SubscribeLocalEvent<EntityHeaterComponent, ItemPlacedEvent>(OnItemPlaced);
        SubscribeLocalEvent<EntityHeaterComponent, HeaterSettingChangedEvent>(OnHeaterSettingChanged);
        SubscribeLocalEvent<EntityHeaterComponent, ComponentShutdown>(OnHeaterRemoved);
        //SS220-grill-update end
    }

    private void OnMapInit(Entity<EntityHeaterComponent> ent, ref MapInitEvent args)
    {
        // Set initial power level
        if (TryComp<ApcPowerReceiverComponent>(ent, out var power))
            power.Load = SettingPower(ent.Comp.Setting, ent.Comp.Power);
    }

    //SS220-grill-update begin
    private void OnHeaterSettingChanged(Entity<EntityHeaterComponent> ent, ref HeaterSettingChangedEvent args)
    {
        if (!TryComp<ItemPlacerComponent>(ent, out var placer))
            return;

        switch (args.Setting)
        {
            case EntityHeaterSetting.Off:
                _audio.Stop(ent.Comp.GrillingAudioStream);

                foreach (var item in placer.PlacedEntities)
                {
                    RemComp<GrillingVisualComponent>(item);
                }

                break;

            default:

                GrillVisualsUpdate(ent); //SS220-grill-update-2
                break;
        }
    }

    private void OnItemRemoved(Entity<EntityHeaterComponent> ent, ref ItemRemovedEvent args)
    {
        RemComp<GrillingVisualComponent>(args.OtherEntity);
        GrillVisualsUpdate(ent); //SS220-grill-update-2
    }

    private void OnItemPlaced(Entity<EntityHeaterComponent> ent, ref ItemPlacedEvent args)
    {
        if (ent.Comp.Setting is EntityHeaterSetting.Off)
            return;

        GrillVisualsUpdate(ent); //SS220-grill-update-2
    }

    // Grill is no longer a grill, clear the visuals and audio, just in case
    private void OnHeaterRemoved(Entity<EntityHeaterComponent> ent, ref ComponentShutdown args)
    {
        _audio.Stop(ent.Comp.GrillingAudioStream);

        if (TryComp<ItemPlacerComponent>(ent, out var placer))
        {
            foreach (var item in placer.PlacedEntities)
            {
                RemComp<GrillingVisualComponent>(item);
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
            }
        }
    }

    /// <remarks>
    /// <see cref="ApcPowerReceiverComponent"/> doesn't exist on the client, so we need
    /// this server-only override to handle setting the network load.
    /// </remarks>
    protected override void ChangeSetting(Entity<EntityHeaterComponent> ent, EntityHeaterSetting setting, EntityUid? user = null)
    {
        base.ChangeSetting(ent, setting, user);

        if (!TryComp<ApcPowerReceiverComponent>(ent, out var power))
            return;

        power.Load = SettingPower(setting, ent.Comp.Power);
    }

    //SS220-grill-update-2 begin
    private void GrillVisualsUpdate(Entity<EntityHeaterComponent> grill)
    {
        var showVisuals = false;

        if (TryComp<ItemPlacerComponent>(grill, out var placer))
        {
            foreach (var item in placer.PlacedEntities)
            {
                if (grill.Comp.Setting != EntityHeaterSetting.Off &&
                    _whitelistSystem.IsWhitelistPass(grill.Comp.HeatingVisuals?.Whitelist, item) &&
                    !_tagSystem.HasTag(item, $"Cooked"))
                {
                    showVisuals = true;
                    var grillVisuals = EnsureComp<GrillingVisualComponent>(item);
                    grillVisuals.GrillingSprite = grill.Comp.HeatingVisuals?.Sprite;
                    Dirty(item, grillVisuals);
                }
                else
                {
                    RemComp<GrillingVisualComponent>(item);
                }
            }
        }

        if (showVisuals)
            PlayGrillAudio(grill);
        else
            _audio.Stop(grill.Comp.GrillingAudioStream);
    }
    //SS220-grill-update-2 end

    //SS220-grill-update begin
    private void PlayGrillAudio(Entity<EntityHeaterComponent> grill)
    {
        if (_audio.IsPlaying(grill.Comp.GrillingAudioStream))
            return;

        var audioParams = AudioParams.Default.WithMaxDistance(10f).WithLoop(true);
        grill.Comp.GrillingAudioStream = _audio.PlayPvs(grill.Comp.GrillSound, grill, audioParams)?.Entity;
    }
    //SS220-grill-update end
}
