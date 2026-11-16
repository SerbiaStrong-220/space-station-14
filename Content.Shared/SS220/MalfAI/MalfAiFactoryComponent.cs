// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.MalfAI;

[RegisterComponent]
public sealed partial class MalfAiFactoryComponent : Component
{
    [ViewVariables]
    public EntityUid? VictimMind;

    [ViewVariables]
    public EntityUid? PendingBody;

    [ViewVariables]
    public TimeSpan ConversionDue;

    [ViewVariables]
    public TimeSpan FinishDue;

    [ViewVariables]
    public TimeSpan? PausedAt;

    [DataField]
    public float ConversionTime = 60f;

    [DataField]
    public float PostConversionDelay = 3f;

    [DataField]
    public float InsertionDelayPerMass = 0.1f;

    [ViewVariables]
    public TimeSpan NextDenyPopup;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ActiveMalfAiFactoryComponent : Component;

[RegisterComponent]
public sealed partial class FinishingMalfAiFactoryComponent : Component;

[Serializable, NetSerializable]
public enum MalfAiFactoryVisuals : byte
{
    Status,
}

[Serializable, NetSerializable]
public enum MalfAiFactoryStatus : byte
{
    Off,
    Idle,
    Active,
    Finishing,
}
