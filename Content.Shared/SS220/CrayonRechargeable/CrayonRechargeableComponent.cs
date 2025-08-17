namespace Content.Shared.SS220.CrayonRechargeable;

[RegisterComponent]
public sealed partial class CrayonRechargeableComponent : Component
{
    [DataField]
    public int ChargesPerWait { get; set; } = 1;

    [DataField]
    public TimeSpan WaitingForCharge { get; set; } = TimeSpan.FromSeconds(2.3f);

    public TimeSpan NextChargeTime = TimeSpan.Zero;
}
