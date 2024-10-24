namespace Content.Shared.SS220.RadiationDecrease;

/// <summary>
///     Decrease radiation per second in Damaged RTG
/// </summary>
[RegisterComponent]
public sealed partial class RadiationDecreaseComponent : Component
{
    public readonly int TotalAliveTime = 1200; // 20 minutes

    public readonly TimeSpan CoolDown = TimeSpan.FromSeconds(1f);

    public TimeSpan LastTimeDecreaseRad = TimeSpan.Zero;
    public TimeSpan LastTimeDecreaseSupply = TimeSpan.Zero;

    public float Intensity = 0; // radiation capacity in RadiationSourceComponent
    public float Supply = 0; // powersupp in PowerSupplierComponent

}
