using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.MachineStorage.SmartFridge
{
    /// <summary>
    /// Handles generic storage with window, such as backpacks.
    /// </summary>
    [RegisterComponent, NetworkedComponent]
    public sealed partial class SmartFridgeComponent : Component
    {
        /// <summary>
        /// Whitelist for entities that can go into the storage.
        /// </summary>
        [DataField("whitelist")]
        public EntityWhitelist? Whitelist;

        /// <summary>
        /// Blacklist for entities that can go into storage.
        /// </summary>
        [DataField("blacklist")]
        public EntityWhitelist? Blacklist;

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

    [Serializable, NetSerializable]
    public sealed class SmartFridgeInteractWithItemEvent : BoundUserInterfaceMessage
    {
        public readonly NetEntity InteractedItemUID;
        public SmartFridgeInteractWithItemEvent(NetEntity interactedItemUID)
        {
            InteractedItemUID = interactedItemUID;
        }
    }
}
