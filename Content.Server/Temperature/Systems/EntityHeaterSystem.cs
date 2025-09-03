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
        SubscribeLocalEvent<EntityHeaterComponent, ItemRemovedEvent>(OnItemRemovedFromTheGrill);
        SubscribeLocalEvent<EntityHeaterComponent, ItemPlacedEvent>(OnItemPlacedOnTheGrill);
        SubscribeLocalEvent<EntityHeaterComponent, HeaterSettingChangedEvent>(OnHeaterSettingChanged);
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
        // If the grill has been turned off and there is still food on the grill
        // Stop grilling audio, disable grilling visuals for all food on the grill
        if (args.Setting == EntityHeaterSetting.Off)
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
        else // Grill has been turned on
        {
            // Processing each item on the grill
            if (TryComp<ItemPlacerComponent>(ent, out var placer))
            {
                foreach (var item in placer.PlacedEntities)
                {
                    // Skip visuals, if entity can't be cooked on the grill or if entity is already cooked
                    if(_whitelistSystem.IsWhitelistFail(ent.Comp.HeatingAnimationWhitelist, item)
                       || _tagSystem.HasTag(item, $"Cooked"))
                        return;

                    // We are grilling
                    if (!HasComp<GrillingVisualComponent>(item))
                    {
                        var grillingVisual = AddComp<GrillingVisualComponent>(item);

                        var ev = new HeaterVisualsEvent(GetNetEntity(item),
                            ent.Comp.GrillingSprite,
                            grillingVisual.GrillingLayer);

                        RaiseNetworkEvent(ev);
                    }

                    // Maybe not the best way of doing it, but it'll have to do
                    PlayGrillAudio(ent);
                }
            }
        }
    }

    private void OnItemRemovedFromTheGrill(Entity<EntityHeaterComponent> ent, ref ItemRemovedEvent args)
    {
        // Disable grill smoke visuals
        RemComp<GrillingVisualComponent>(args.OtherEntity);

        if (TryComp<ItemPlacerComponent>(ent, out var placer))
        {
            // When removing item from the grill, if it is the last one -> stop playing audio
            if (placer.PlacedEntities.Count == 0)
            {
                _audio.Stop(ent.Comp.GrillingAudioStream);
            }
        }
    }

    private void OnItemPlacedOnTheGrill(Entity<EntityHeaterComponent> ent, ref ItemPlacedEvent args)
    {
        // Items placed on turned off grill
        if (ent.Comp.Setting == EntityHeaterSetting.Off)
            return;

        // I know this looks bad, but I couldn't figure out better way to check if everything is cooked on the grill
        var isEverythingCooked = true;
        // Food just been cooked (or cooked food was placed on the grill)
        if (_tagSystem.HasTag(args.OtherEntity, $"Cooked"))
        {
            // If there is still uncooked food on the grill -> break
            if (TryComp<ItemPlacerComponent>(ent, out var placer))
            {
                foreach (var item in placer.PlacedEntities)
                {
                    if (!_tagSystem.HasTag(item, "Cooked"))
                        isEverythingCooked = false;
                    break;
                }
            }

            // Otherwise -> stop grilling audio
            if(isEverythingCooked) _audio.Stop(ent.Comp.GrillingAudioStream);
        }

        // Skip visuals, if entity can't be cooked on the grill or if entity is already cooked
        if(_whitelistSystem.IsWhitelistFail(ent.Comp.HeatingAnimationWhitelist, args.OtherEntity)
           || _tagSystem.HasTag(args.OtherEntity, $"Cooked"))
        return;

        PlayGrillAudio(ent);

        // We are grilling
        if (!HasComp<GrillingVisualComponent>(args.OtherEntity))
        {
            var grillingVisual = AddComp<GrillingVisualComponent>(args.OtherEntity);

            var ev = new HeaterVisualsEvent(GetNetEntity(args.OtherEntity),
                ent.Comp.GrillingSprite,
                grillingVisual.GrillingLayer);

            RaiseNetworkEvent(ev);
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

    //SS220-grill-update begin
    private void PlayGrillAudio(Entity<EntityHeaterComponent> grill)
    {
        // If there is no grilling audio playing -> play grilling audio
        if (!_audio.IsPlaying(grill.Comp.GrillingAudioStream))
        {
            grill.Comp.GrillingAudioStream = _audio.PlayPvs(grill.Comp.GrillSound,
                    grill,
                    AudioParams.Default.WithMaxDistance(10f).WithLoop(true))
                ?.Entity;
        }
    }
    //SS220-grill-update end
}
