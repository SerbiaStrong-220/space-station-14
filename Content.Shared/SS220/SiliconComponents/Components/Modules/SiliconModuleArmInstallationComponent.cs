// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.SiliconComponents;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SiliconModuleArmInstallationComponent : Component
{
    [DataField]
    public EntProtoId? Item;

    [DataField, AutoNetworkedField]
    public EntityUid StoredItem = new();

    [DataField, AutoNetworkedField]
    public bool Spawned;

    //[DataField, AutoNetworkedField]
    //public ContainerSlot ItemContainer = new();

    [DataField]
    public string HoldingContainerPrefix = "synth_arm_installation";

    [DataField]
    public string HoldingContainerHandId = "right";

    [DataField]
    public EntProtoId InstallationToggleAction = "ActionToggleArmInstallation";

    [DataField, AutoNetworkedField]
    public EntityUid? InstallationToggleEntity;

    [DataField]
    public SoundSpecifier ToggleSound = new SoundPathSpecifier("/Audio/Effects/stealthoff.ogg");
}

public sealed partial class SiliconModuleArmInstallationToggledEvent : InstantActionEvent;
