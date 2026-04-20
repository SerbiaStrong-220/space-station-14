using Content.Shared.Chemistry.Reaction;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.BeerUpdate.FermentationBarrel
{
    [Serializable, NetSerializable]
    public enum FermentationBarrelUiKey
    {
        Key
    }

    public static class SharedFermentationBarrel
    {
        public const string SolutionName = "barrel";
    }

    [Serializable, NetSerializable]
    public sealed class FermentationBarrelToggleEvent : BoundUserInterfaceMessage { }

    [Serializable, NetSerializable]
    public sealed class FermentationBarrelModeChangeEvent : BoundUserInterfaceMessage { }

    public sealed class FermentationBarrelReactionAttemptEvent : CancellableEntityEventArgs
    {
        public ReactionPrototype Reaction;
        public FermentationBarrelReactionAttemptEvent(ReactionPrototype reaction)
        {
            Reaction = reaction;
        }
    }

    [Serializable, NetSerializable]
    public sealed class FermentationBarrelInterfaceState : BoundUserInterfaceState
    {
        public bool IsActive;
        public float ElapsedTime;
        public ReagentQuantity[]? Reagents;

        public FermentationBarrelInterfaceState(bool isActive, float elapsedTime, ReagentQuantity[]? reagents = null)
        {
            IsActive = isActive;
            ElapsedTime = elapsedTime;
            Reagents = reagents;
        }
    }
}
