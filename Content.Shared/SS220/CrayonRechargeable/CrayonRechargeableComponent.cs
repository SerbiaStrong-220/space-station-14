namespace Content.Shared.SS220.CrayonRechargeable
{
    [RegisterComponent]
    public sealed partial class CrayonRechargeableComponent : Component
    {
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public int ChargesPerWait { get; set; } = 1;

        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public float WaitingForCharge { get; set; } = 2.3f;

        public TimeSpan NextChargeTime = TimeSpan.FromSeconds(0f);
    }
}
