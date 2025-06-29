namespace Content.Shared.SS220.CrayonRechargeable
{
    [RegisterComponent]
    public sealed partial class CrayonRechargeableComponent : Component
    {
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public int ChargesPerWait { get; set; } = 1;

        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public TimeSpan WaitingForCharge { get; set; } = TimeSpan.FromSeconds(2.3f);

        public TimeSpan NextChargeTime = TimeSpan.Zero;
    }
}
