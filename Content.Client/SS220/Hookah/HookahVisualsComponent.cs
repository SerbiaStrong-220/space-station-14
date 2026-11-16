namespace Content.Client.SS220.Hookah;

[RegisterComponent]
public sealed partial class HookahVisualsComponent : Component
{
    [DataField]
    public string UnlitState = "icon";

    [DataField]
    public string UnlitNoHoseState = "icon-no-hose";

    [DataField]
    public string CoalUnlitState = "icon-coal";

    [DataField]
    public string CoalUnlitNoHoseState = "icon-coal-no-hose";

    [DataField]
    public string CoalLitState = "icon-lit";

    [DataField]
    public string CoalLitNoHoseState = "icon-lit-no-hose";
}
