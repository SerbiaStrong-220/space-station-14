// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
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

    [DataField]
    public List<PartType> PartsRequiredToOperate = new List<PartType> { PartType.Servo, PartType.Spine };

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

    [DataField]
    public ProtoId<AlertPrototype> BatteryAlert = "BorgBattery";

    [DataField]
    public ProtoId<AlertPrototype> NoBatteryAlert = "BorgBatteryNone";

    [DataField]
    [AutoNetworkedField]
    public FixedPoint2 ChargeToUse = 0.07;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan NextBatteryUpdate = TimeSpan.Zero;

    [DataField]
    public SoundSpecifier? UnlockSound = new SoundPathSpecifier("/Audio/Machines/door_lock_off.ogg")
    {
        Params = AudioParams.Default.WithVolume(-5f),
    };

    [DataField]
    public SoundSpecifier? LockSound = new SoundPathSpecifier("/Audio/Machines/door_lock_on.ogg")
    {
        Params = AudioParams.Default.WithVolume(-5f)
    };
}

public enum PartType : byte
{
    Optics,
    Servo,
    Brain,
    Spine,
    Drive,
    ALL,
    NONE
}
