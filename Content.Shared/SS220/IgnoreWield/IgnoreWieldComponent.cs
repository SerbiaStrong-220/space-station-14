namespace Content.Shared.SS220.IgnoreWield;

[RegisterComponent]
public sealed partial class IgnoreWieldComponent : Component
{
    [DataField]
    public bool IgnoreWeaponStats = true;
}
