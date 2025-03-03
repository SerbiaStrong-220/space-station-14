// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Actions;
using Content.Shared.SS220.SmartGasMask.Prototype;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.SmartGasMask;

/// <summary>
/// This is used for automatic notifications, selects via radial menu
/// </summary>
[RegisterComponent]
public sealed partial class SmartGasMaskComponent : Component
{
    [DataField]
    public List<ProtoId<AlertSmartGasMaskPrototype>> SelectablePrototypes = [];

    [DataField]
    public EntProtoId SmartGasMaskAction = "ActionSmartGasMask";

    [DataField]
    public EntityUid? SmartGasMaskActionEntity;

    public Dictionary<ProtoId<AlertSmartGasMaskPrototype>, TimeSpan> NextChargeTime = new();
}

public sealed partial class SmartGasMaskOpenEvent : InstantActionEvent;
