// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.AstralLeap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AstralLeapComponent : Component
{
    [DataField]
    public EntProtoId AstralAction = "ActionMiGoAstral2";//ToDo_SS220 make it required Datafield?

    [ViewVariables, AutoNetworkedField]
    public EntityUid? AstralActionEntity;

    [DataField]
    public ProtoId<AlertPrototype> AstralAlert = "MiGoAstralAlert";
}

[NetSerializable, Serializable]
public enum AstralLeapTimerVisualLayers : byte
{
    Digit1,
    Digit2
}
