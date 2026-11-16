// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.PolymorphTimer;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PolymorphTimerComponent : Component
{
    [DataField(required: true)]
    public ProtoId<AlertPrototype> PolymorphTimerAlert = "MiGoAstralAlert";

    [ViewVariables, AutoNetworkedField]
    public int AlertTime;
}

[NetSerializable, Serializable]
public enum PolymorphTimerVisualLayers : byte
{
    Digit1,
    Digit2
}
