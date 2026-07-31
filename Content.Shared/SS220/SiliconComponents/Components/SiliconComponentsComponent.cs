// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.SS220.SiliconComponents;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
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

    [DataField, AutoNetworkedField]
    public int MaxModuleSpace = 50;

    [DataField]
    public string BatterySlotId = "cell_slot";

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

    [DataField]
    [AutoNetworkedField]
    public FixedPoint2 ChargeToUse = 0.07;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextBatteryUpdate = TimeSpan.Zero;

}

public enum PartType : byte
{
    Optics,
    Servo,
    Brain,
    Spine,
    Drive
}
