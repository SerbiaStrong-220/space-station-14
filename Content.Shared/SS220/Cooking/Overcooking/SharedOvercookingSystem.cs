// SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Examine;
using Content.Shared.Nutrition.Components;
using Content.Shared.Tag;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Cooking.Overcooking;

/// <summary>
/// Handles cooked food overcooking, independent of the cooking source.
/// </summary>
public sealed partial class SharedOvercookingSystem : EntitySystem
{
    private static readonly ProtoId<TagPrototype> CookedTag = "Cooked";

    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OvercookingComponent, ExaminedEvent>(OnExamined);
    }

    public bool UpdateOvercooking(EntityUid uid, float frameTime)
    {
        if (!HasComp<BeingCookedComponent>(uid) || !CanBeOvercooked(uid))
            return false;

        var overcooking = EnsureComp<OvercookingComponent>(uid);
        overcooking.CurrentOvercookTime += frameTime;
        Dirty(uid, overcooking);

        // Overcooked
        if (overcooking.CurrentOvercookTime >= overcooking.TimeToOvercook)
        {
            var newEnt = Spawn(overcooking.OvercookedEntity, _transformSystem.GetMapCoordinates(uid));
            _transformSystem.SetLocalRotation(newEnt, Angle.Zero);

            _audio.PlayPvs(overcooking.OvercookedSound, newEnt);

            PredictedDel(uid);
        }

        return true;
    }

    public bool CanBeOvercooked(EntityUid uid)
    {
        return HasComp<EdibleComponent>(uid) && _tag.HasTag(uid, CookedTag);
    }

    private void OnExamined(Entity<OvercookingComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (HasComp<BeingCookedComponent>(ent))
            args.PushMarkup(Loc.GetString("grillable-state-overcooking"));
        else if (ent.Comp.CurrentOvercookTime > ent.Comp.MinOvercookingTime)
            args.PushMarkup(Loc.GetString("grillable-state-overcooked"));
    }
}
