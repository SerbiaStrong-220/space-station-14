// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.SS220.MalfAI;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class MalfAiDoomsdayWaveComponent : Component
{
    [AutoNetworkedField]
    public Vector2 Origin;

    [AutoNetworkedField]
    public MapId MapId;

    [AutoNetworkedField, AutoPausedField]
    public TimeSpan StartTime;

    [AutoNetworkedField]
    public float Speed = 5f;

    [AutoNetworkedField]
    public float MaxRadius;

    [DataField, AutoNetworkedField]
    public int RingCount = 3;

    [DataField, AutoNetworkedField]
    public float RingDelay = 2f;

    [DataField]
    public float ShockDamage = 250f;

    [DataField]
    public float ElectrocuteDamage = 40f;

    [DataField]
    public float ElectrocuteSeconds = 2.5f;

    [DataField]
    public float FxStep = 2.4f;

    [DataField]
    public int FxCount = 6;
}
