using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.SmartFridge
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class SmartFridgeComponent : Component
    {
        [Serializable, NetSerializable]
        public sealed class SmartFridgeInsertItemMessage : BoundUserInterfaceMessage
        {
        }

        [Serializable, NetSerializable]
        public enum SmartFridgeUiKey
        {
            Key,
        }
    }
}
