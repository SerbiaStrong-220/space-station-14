// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;
using System.Numerics;

namespace Content.Shared.SS220.Shitspawn.AshDrake;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AshDrakeMeteorFallingComponent : Component
{
    [DataField, AutoNetworkedField] public Vector2 Target;
    [DataField, AutoNetworkedField] public float Speed = 8f;
}
