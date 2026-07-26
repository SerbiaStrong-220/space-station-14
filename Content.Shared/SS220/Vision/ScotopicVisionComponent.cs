// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Vision;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ScotopicVisionComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Radius = 5f;

    [DataField, AutoNetworkedField]
    public float Energy = 0.1f;

    [DataField, AutoNetworkedField]
    public float Softness = 1f;

    [DataField, AutoNetworkedField]
    public Color Color = Color.FromHex("#ffffff");

    [DataField, AutoNetworkedField]
    public string? MaskPath;
}
