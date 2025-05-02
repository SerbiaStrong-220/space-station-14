using Content.Shared.Storage;

namespace Content.Server.SS220.Clothing
{
    /// <summary>
    ///     Limits the number of uses and spawns items when used in got unequiped.
    /// </summary>
    [RegisterComponent]
    public sealed partial class LimitiedEquipComponent : Component
    {
        /// <summary>
        ///     The list of entities to spawn, with amounts and orGroups.
        /// </summary>
        [DataField("items", required: true)]
        public List<EntitySpawnEntry> Items = new();

        /// <summary>
        ///     How many uses before the item should delete itself.
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite)]
        public int Uses = 1;
    }
}
