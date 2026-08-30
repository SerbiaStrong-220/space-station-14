namespace Content.Client.SS220.HookahElectric;

[RegisterComponent]
public sealed partial class HookahElectricVisualsComponent : Component
{
    [DataField]
    public string OffState = "icon";

    [DataField]
    public string OnState = "icon-enabled";

    [DataField]
    public string LeftHoseState = "left-hose";

    [DataField]
    public string RightHoseState = "right-hose";
}
