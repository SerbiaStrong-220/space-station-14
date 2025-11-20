// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Placeable;
using Content.Shared.SS220.EntityEffects.Effects;
using Content.Shared.Temperature;
using Content.Shared.Temperature.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.EntitySerialization;
using Robust.Shared.Network;

namespace Content.Shared.SS220.Cooking.Grilling;

/// <summary>
/// Handles <see cref="GrillComponent"/> events.
/// </summary>
public abstract class SharedGrillSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _net = default!;
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
        ent.Comp.IsGrillOn = args.Setting != EntityHeaterSetting.Off;
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
            }
        }
    }

    private void OnItemRemoved(Entity<GrillComponent> ent, ref ItemRemovedEvent args)
    {
        RemComp<GrillingVisualComponent>(args.OtherEntity);

        if (TryComp<GrillableComponent>(args.OtherEntity, out var grillable))
            grillable.IsCooking = false;

        UpdateGrillVisuals(ent);
    }

    private void OnItemPlaced(Entity<GrillComponent> ent, ref ItemPlacedEvent args)
    {
        if (!ent.Comp.IsGrillOn)
            return;

        UpdateGrillVisuals(ent);
    }

    private void UpdateGrillVisuals(Entity<GrillComponent> grill)
    {
        var playAudio = false;

        if (TryComp<ItemPlacerComponent>(grill, out var placer))
        {
            foreach (var item in placer.PlacedEntities)
            {
                if (!TryComp<GrillableComponent>(item, out var grillable))
                    continue;

                if (grill.Comp.IsGrillOn)
                {
                    playAudio = true;
                    var grillVisuals = EnsureComp<GrillingVisualComponent>(item);
                    grillVisuals.GrillingSprite = grill.Comp.GrillingSprite;
                    grillable.IsCooking = true;
                }
                else
                {
                    RemComp<GrillingVisualComponent>(item);
                    grillable.IsCooking = false;
                }

                Dirty(item, grillable);
            }
        }

        if (_net.IsServer)
        {
            if(playAudio)
                PlayGrillAudio(grill);
            else
                _audio.Stop(grill.Comp.GrillingAudioStream);
        }
    }

    private void PlayGrillAudio(Entity<GrillComponent> grill)
    {
        if (_audio.IsPlaying(grill.Comp.GrillingAudioStream))
            return;

        var audioParams = AudioParams.Default.WithLoop(true);
        grill.Comp.GrillingAudioStream = _audio.PlayPvs(grill.Comp.GrillSound, grill, audioParams)?.Entity;
    }
}

[ByRefEvent]
public readonly record struct CookTimeChanged(EntityUid GrilledEntity);
