using Content.Server.Power.EntitySystems;
using Content.Shared.Guidebook;

namespace Content.Server.Power.Components
{
    /// <summary>
    ///     Battery node on the pow3r network. Needs other components to connect to actual networks.
    /// </summary>
    [RegisterComponent]
    [Virtual]
    [Access(typeof(BatterySystem))]
    public partial class BatteryComponent : Component
    {
        public string SolutionName = "battery";

        /// <summary>
        /// Maximum charge of the battery in joules (ie. watt seconds)
        /// </summary>
        [DataField]
        [GuidebookData]
        public float MaxCharge;

        /// <summary>
        /// Current charge of the battery in joules (ie. watt seconds)
        /// </summary>
        [DataField("startingCharge")]
        public float CurrentCharge;

        /// <summary>
        /// The price per one joule. Default is 1 credit for 10kJ.
        /// </summary>
        [DataField]
        public float PricePerJoule = 0.0001f;

        //SS220-smes-overcharge begin
        /// <summary>
        /// Use this if you set current charge more than max charge.
        /// Will be false if current charge drops below than max charge.
        /// Also blocks getting more charge when true.
        /// </summary>
        [DataField]
        public bool IsOvercharged = false;
        //SS220-smes-overcharge end
    }

    /// <summary>
    ///     Raised when a battery's charge or capacity changes (capacity affects relative charge percentage).
    /// </summary>
    [ByRefEvent]
    public readonly record struct ChargeChangedEvent(float Charge, float MaxCharge);

    /// <summary>
    ///     Raised when it is necessary to get information about battery charges.
    /// </summary>
    [ByRefEvent]
    public sealed class GetChargeEvent : EntityEventArgs
    {
        public float CurrentCharge;
        public float MaxCharge;
    }

    /// <summary>
    ///     Raised when it is necessary to change the current battery charge to a some value.
    /// </summary>
    [ByRefEvent]
    public sealed class ChangeChargeEvent : EntityEventArgs
    {
        public float OriginalValue;
        public float ResidualValue;

        public ChangeChargeEvent(float value)
        {
            OriginalValue = value;
            ResidualValue = value;
        }
    }
}
