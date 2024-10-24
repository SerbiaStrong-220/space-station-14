using Robust.Shared.Prototypes;

namespace Content.Shared.Access
{
    /// <summary>
    ///     Defines a single access level that can be stored on ID cards and checked for.
    /// </summary>
    [Prototype("accessLevel")]
    public sealed partial class AccessLevelPrototype : IPrototype
    {
        [ViewVariables]
        [IdDataField]
        public string ID { get; private set; } = default!;

        /// <summary>
        ///     The player-visible name of the access level, in the ID card console and such.
        /// </summary>
        [DataField("name")]
        public string? Name { get; set; }

        //SS220-door-electronic-configurator begin
        /// <summary>
        ///     The ability to change access using the network configurator or multitool.
        /// </summary>
        [DataField("canBeSwitchableInReader")]
        public bool CanBeSwitchableInReader { get; set; } = true;
        //SS220-door-electronic-configurator begin

        public string GetAccessLevelName()
        {
            if (Name is { } name)
                return Loc.GetString(name);

            return ID;
        }
    }
}
