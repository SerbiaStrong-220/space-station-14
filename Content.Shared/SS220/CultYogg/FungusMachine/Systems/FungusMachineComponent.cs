using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.SS220.CultYogg.FungusMachine.Systems
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
    public sealed partial class FungusMachineComponent : Component
    {
        public const string ContainerId = "FungusMachine";

        /// <summary>
        /// PrototypeID for the Fungus machine's inventory, see <see cref="FungusMachineInventoryPrototype"/>
        /// </summary>
        [DataField("pack", customTypeSerializer: typeof(PrototypeIdSerializer<FungusMachineInventoryPrototype>), required: true)]
        public string PackPrototypeId = string.Empty;

        [ViewVariables]
        public Dictionary<string, FungusMachineInventoryEntry> Inventory = new();

        [DataField("actionEntity")]
        [AutoNetworkedField]
        public EntityUid? ActionEntity;

        /// <summary>
        ///     Container of unique entities stored inside this Fungus machine.
        /// </summary>
        [ViewVariables] public Container Container = default!;
    }

    [Serializable, NetSerializable]
    public sealed class FungusMachineInventoryEntry
    {
        [ViewVariables(VVAccess.ReadWrite)]
        public string Id;
        public FungusMachineInventoryEntry( string id, uint amount)
        {
            Id = id;
        }
    }
}
