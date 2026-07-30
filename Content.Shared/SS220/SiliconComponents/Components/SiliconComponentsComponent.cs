// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.Alert;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.SiliconComponents;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SiliconComponentsComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Online;

    [DataField]
    public Dictionary<PartType, ContainerSlot> Parts = new Dictionary<PartType, ContainerSlot>();

    #region Modules
    [DataField, AutoNetworkedField]
    public EntityWhitelist? ModuleWhitelist;

    [DataField, AutoNetworkedField]
    public int ModuleSpace = 50;

    [DataField]
    public string ModuleContainerId = "silicon_component_modules";

    [ViewVariables]
    public Container ModuleContainer = default!;

    [ViewVariables]
    public int ModuleCount => ModuleContainer.ContainedEntities.Count;
    #endregion

    #region Visuals
    [DataField]
    public string HasMindState = string.Empty;

    [DataField]
    public string NoMindState = string.Empty;
    #endregion

    /// <summary>
    /// The battery charge alert.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> BatteryAlert = "BorgBattery";

    /// <summary>
    /// The alert for a missing battery.
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> NoBatteryAlert = "BorgBatteryNone";
}

public enum PartType : byte
{
    Optics,
    Servo,
    Brain,
    Spine,
    Drive
}
