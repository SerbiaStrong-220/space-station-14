using Robust.Shared.GameStates;

namespace Content.Shared.SS220.FractWar;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ShuttleConsolePointsComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Fraction = string.Empty;

    [DataField, AutoNetworkedField]
    public float PointsOnDestroy = 500f;
}
