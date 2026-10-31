namespace Content.Shared.SS220.IgnoreWield;

[RegisterComponent]
public sealed partial class IgnoreWieldOnEquipComponent : Component
{
    [DataField]
    public bool IgnoreWeaponStats = true;
}
