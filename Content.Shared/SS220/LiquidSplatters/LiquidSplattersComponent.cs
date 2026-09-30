using Robust.Shared.GameStates;

namespace Content.Shared.SS220.LiquidSplatters;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class LiquidSplattersComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = false;

    [DataField, AutoNetworkedField]
    public float Intensity = .0f;

    [DataField, AutoNetworkedField]
    public Color Color = new(.65f, 0f, 0f);

    [DataField, AutoNetworkedField]
    public float ColorDarkness = .2f;

    [DataField, AutoNetworkedField]
    public String ContainerName = "splatters";

    [DataField, AutoNetworkedField]
    public int MaxVolume = 20;
}
