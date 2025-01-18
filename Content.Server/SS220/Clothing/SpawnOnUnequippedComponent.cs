using Content.Shared.Storage;

namespace Content.Server.SS220.Clothing
{
    /// <summary>
    ///     Spawns items when used in got unequiped.
    /// </summary>
    [RegisterComponent]
    public sealed partial class SpawnOnUnequippedComponent : Component
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
