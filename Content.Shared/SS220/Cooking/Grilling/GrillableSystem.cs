// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Examine;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.SS220.Cooking.Grilling;

/// <summary>
/// This handles the grilling process of grillable entity
/// </summary>
public sealed class GrillableSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GrillableComponent, CookTimeChanged>(OnCookTimeChanged);
        SubscribeLocalEvent<GrillableComponent, ExaminedEvent>(OnGrillableExamined);
    }

    private void OnGrillableExamined(Entity<GrillableComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (ent.Comp.CurrentCookTime == 0)
        {
            args.PushMarkup(Loc.GetString("grillable-state-begin", ("target", Loc.GetEntityData(ent.Comp.CookingResult).Name)));
        }
        else if (ent.Comp.CurrentCookTime <= ent.Comp.TimeToCook * 0.75f)
        {
            args.PushMarkup(Loc.GetString("grillable-state-in-process"));
        }
        else if (ent.Comp.CurrentCookTime <= ent.Comp.TimeToCook)
        {
            args.PushMarkup(Loc.GetString("grillable-state-near-end"));
        }
    }

    private void OnCookTimeChanged(Entity<GrillableComponent> ent, ref CookTimeChanged args)
    {
        // Cooking is done
        if (ent.Comp.CurrentCookTime >= ent.Comp.TimeToCook)
        {
            _audio.PlayPvs(ent.Comp.CookingDoneSound, ent, new AudioParams());

            EntityManager.Spawn(ent.Comp.CookingResult,
                _transformSystem.GetMapCoordinates(ent),
                null,
                _transformSystem.GetWorldRotation(ent));

            PredictedDel(ent.Owner);
        }
    }
}
