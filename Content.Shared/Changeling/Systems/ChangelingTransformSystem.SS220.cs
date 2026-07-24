using Content.Shared.Changeling.Components;

namespace Content.Shared.Changeling.Systems;

public sealed partial class ChangelingTransformSystem
{
    /// <summary>
    /// Moves the configured transformation ability to a new body without carrying transient action or sound entities.
    /// </summary>
    public bool TryTransferTo(Entity<ChangelingTransformComponent?> source, EntityUid target)
    {
        if (!Resolve(source, ref source.Comp, false) ||
            TerminatingOrDeleted(target) ||
            HasComp<ChangelingTransformComponent>(target))
        {
            return false;
        }

        AddComp(target, new ChangelingTransformComponent
        {
            ChangelingTransformAction = source.Comp.ChangelingTransformAction,
            TransformWindup = source.Comp.TransformWindup,
            ChemicalCost = source.Comp.ChemicalCost,
            TransformAttemptNoise = source.Comp.TransformAttemptNoise,
            TransformCloningSettings = source.Comp.TransformCloningSettings,
        });
        RemComp<ChangelingTransformComponent>(source.Owner);
        return true;
    }
}
