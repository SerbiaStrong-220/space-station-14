// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Placeable;
using Content.Shared.SS220.EntityEffects.Effects;
using Content.Shared.SS220.Cooking.Overcooking;
using Content.Shared.Temperature;
using Content.Shared.Temperature.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;

namespace Content.Shared.SS220.Cooking.Grilling;

/// <summary>
/// Handles <see cref="GrillComponent"/> events.
/// </summary>
public abstract partial class SharedGrillSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private readonly SharedOvercookingSystem _overcooking = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GrillComponent, SharedEntityHeaterSystem.HeaterSettingChangedEvent>(OnHeaterSettingChanged);
        SubscribeLocalEvent<GrillComponent, ItemPlacedEvent>(OnItemPlaced);
        SubscribeLocalEvent<GrillComponent, ItemRemovedEvent>(OnItemRemoved);
        SubscribeLocalEvent<GrillComponent, ComponentShutdown>(OnGrillRemoved);
    }

    private void OnHeaterSettingChanged(Entity<GrillComponent> ent, ref SharedEntityHeaterSystem.HeaterSettingChangedEvent args)
    {
        ent.Comp.GrillSettings = args.Setting;
        Dirty(ent);

        UpdateGrillVisuals(ent);
    }

    // Grill is no longer a grill, clear the visuals and audio, just in case
    private void OnGrillRemoved(Entity<GrillComponent> ent, ref ComponentShutdown args)
    {
        _audio.Stop(ent.Comp.GrillingAudioStream);

        if (TryComp<ItemPlacerComponent>(ent, out var placer))
        {
            foreach (var item in placer.PlacedEntities)
            {
                RemComp<GrillingVisualComponent>(item);
                RemComp<BeingCookedComponent>(item);
            }
        }
    }

    private void OnItemRemoved(Entity<GrillComponent> ent, ref ItemRemovedEvent args)
    {
        RemComp<GrillingVisualComponent>(args.OtherEntity);
        RemComp<BeingCookedComponent>(args.OtherEntity);

        UpdateGrillVisuals(ent);
    }

    private void OnItemPlaced(Entity<GrillComponent> ent, ref ItemPlacedEvent args)
    {
        if (ent.Comp.GrillSettings == EntityHeaterSetting.Off)
            return;

        UpdateGrillVisuals(ent);
    }

    private void UpdateGrillVisuals(Entity<GrillComponent> grill)
    {
        var playAudio = false;
        if (!TryComp<ItemPlacerComponent>(grill, out var placer))
        {
            _audio.Stop(grill.Comp.GrillingAudioStream);
            return;
        }

        foreach (var item in placer.PlacedEntities)
        {
            if (!HasComp<GrillableComponent>(item) && !_overcooking.CanBeOvercooked(item))
                continue;

            if (grill.Comp.GrillSettings == EntityHeaterSetting.Off)
            {
                RemComp<GrillingVisualComponent>(item);
            }
            else
            {
                playAudio = true;
                var grillVisuals = EnsureComp<GrillingVisualComponent>(item);
                grillVisuals.GrillingSprite = grill.Comp.GrillingSprite;
            }
        }

        _audio.Stop(grill.Comp.GrillingAudioStream);
        if (playAudio && _net.IsServer)
            PlayGrillAudio(grill, GetPitchFromSetting(grill.Comp.GrillSettings));

    }

    private void PlayGrillAudio(Entity<GrillComponent> grill, float pitch)
    {
        var audioParams = grill.Comp.GrillSound.Params.WithPitchScale(pitch);
        grill.Comp.GrillingAudioStream = _audio.PlayPvs(grill.Comp.GrillSound, grill, audioParams)?.Entity;
    }

    private float GetPitchFromSetting(EntityHeaterSetting setting)
    {
        return setting switch
        {
            EntityHeaterSetting.High => 1.5f,
            EntityHeaterSetting.Medium => 1.1f,
            EntityHeaterSetting.Low => 0.9f,
            _ => 1f
        };
    }
}

[ByRefEvent]
public readonly record struct CookTimeChanged(EntityUid GrilledEntity);
