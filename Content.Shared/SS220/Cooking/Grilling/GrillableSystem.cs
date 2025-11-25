// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Atmos.Rotting;
using Content.Shared.Examine;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.SS220.Cooking.Grilling;

/// <summary>
/// This handles the grilling process of grillable entity
/// </summary>
public sealed class GrillableSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

    private const float AlmostDoneCookPercentage = 0.75f;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GrillableComponent, CookTimeChanged>(OnCookTimeChanged);
        SubscribeLocalEvent<GrillableComponent, ExaminedEvent>(OnGrillableExamined);
        SubscribeLocalEvent<GrillableComponent, IsRottingEvent>(OnGrillableRotting);
    }

    // Stop rotting, if food is cooking
    private void OnGrillableRotting(Entity<GrillableComponent> ent, ref IsRottingEvent args)
    {
        args.Handled = ent.Comp.IsCooking;
    }

    private void OnGrillableExamined(Entity<GrillableComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        switch (ent.Comp.CurrentCookTime)
        {
            case <= 0:
                args.PushMarkup(Loc.GetString("grillable-state-begin", ("target", Loc.GetEntityData(ent.Comp.CookingResult).Name)));
                break;
            case var currentCookTime when currentCookTime <= ent.Comp.TimeToCook * AlmostDoneCookPercentage:
                args.PushMarkup(Loc.GetString("grillable-state-in-process"));
                break;
            case var currentCookTime when currentCookTime <= ent.Comp.TimeToCook:
                args.PushMarkup(Loc.GetString("grillable-state-near-end"));
                break;
        }
    }

    private void OnCookTimeChanged(Entity<GrillableComponent> ent, ref CookTimeChanged args)
    {
        // Cooking is done
        if (ent.Comp.CurrentCookTime >= ent.Comp.TimeToCook)
        {
            var newEnt = EntityManager.Spawn(ent.Comp.CookingResult,
                _transformSystem.GetMapCoordinates(ent));

            _audio.PlayPvs(ent.Comp.CookingDoneSound, newEnt);

            PredictedDel(ent.Owner);
        }
    }
}
