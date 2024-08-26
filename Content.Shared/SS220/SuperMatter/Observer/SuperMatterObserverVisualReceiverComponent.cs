// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.SuperMatter.Ui;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.SuperMatter.Observer;

/// <summary> We use this component to mark entities which can receiver </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class SuperMatterObserverVisualReceiverComponent() : Component
{
    [DataField, AutoNetworkedField]
    public string? UnActiveState;
    [DataField, AutoNetworkedField]
    public string? OnState;
    [DataField, AutoNetworkedField]
    public string? WarningState;
    [DataField, AutoNetworkedField]
    public string? DisabledState;
    [DataField, AutoNetworkedField]
    public string? DangerState;
    [DataField, AutoNetworkedField]
    public string? DelaminateState;
}

public enum SuperMatterVisualState
{
    UnActiveState,
    Okay,
    Disable,
    Warning,
    Danger,
    Delaminate,
}
public enum SuperMatterVisualLayers
{
    Lights,
    UnShaded
}
public enum SuperMatterVisuals : byte
{
    VisualState
}
