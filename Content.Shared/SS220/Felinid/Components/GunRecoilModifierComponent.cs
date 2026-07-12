namespace Content.Shared.SS220.Felinid.Components;

[RegisterComponent]
public sealed partial class GunRecoilModifierComponent : Component
{
    [DataField]
    public float KnockdownChanceModifier = 1f;
}
