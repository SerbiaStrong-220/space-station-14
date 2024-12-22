// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.SuperMatter.Ui;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.SuperMatter.Observer;

/// <summary> We use this component to mark entities which can receiver </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class SuperMatterObserverVisualReceiverComponent() : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<SuperMatterVisualLayers, string>? UnActiveState;
    [DataField, AutoNetworkedField]
    public Dictionary<SuperMatterVisualLayers, string>? OnState;
    [DataField, AutoNetworkedField]
    public Dictionary<SuperMatterVisualLayers, string>? WarningState;
    [DataField, AutoNetworkedField]
    public Dictionary<SuperMatterVisualLayers, string>? DisabledState;
    [DataField, AutoNetworkedField]
    public Dictionary<SuperMatterVisualLayers, string>? DangerState;
    [DataField, AutoNetworkedField]
    public Dictionary<SuperMatterVisualLayers, string>? DelaminateState;
    [DataField, AutoNetworkedField]
    public Dictionary<SuperMatterVisualLayers, string>? RandomEvent;
    [DataField, AutoNetworkedField]
    public float RandomEventDuration = 5f;
    public TimeSpan RandomEventTime = default!;
}

public enum SuperMatterVisualState
{
    UnActiveState,
    Okay,
    Disable,
    Warning,
    Danger,
    Delaminate,
    RandomEvent
}
public enum SuperMatterVisualLayers
{
    Shaded,
    Unshaded
}
public enum SuperMatterVisuals : byte
{
    VisualState
}
