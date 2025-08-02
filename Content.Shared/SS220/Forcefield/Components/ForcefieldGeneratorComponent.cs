// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.DeviceLinking;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Forcefield.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ForcefieldGeneratorComponent : Component
{
    /// <summary>
    /// Force field parameters
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public ForcefieldParams ForcefieldParams = new();

    /// <summary>
    /// How much energy it costs per seond to keep the field up
    /// </summary>
    [DataField]
    public float EnergyUpkeep = 183;

    /// <summary>
    /// How much energy it consumes per 1 unit of damage
    /// </summary>
    [DataField]
    public float DamageToEnergyCoefficient = 15;

    /// <summary>
    /// Whether the GENERATOR is active or not
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public bool Active = false;

    /// <summary>
    /// Whether the FIELD is active or not
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public bool FieldEnabled = false;

    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier GeneratorIdleSound = new SoundPathSpecifier("/Audio/SS220/Effects/shield/eshild_loop.ogg");

    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier GeneratorOnSound = new SoundPathSpecifier("/Audio/SS220/Effects/shield/eshild_on.ogg");

    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier GeneratorOffSound = new SoundPathSpecifier("/Audio/SS220/Effects/shield/eshild_off.ogg");

    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public NetEntity? FieldEntity;

    [DataField]
    public ProtoId<SinkPortPrototype> TogglePort = "Toggle";

    [DataField]
    public EntProtoId ShieldProto = "forcefield220";
}

[NetSerializable, Serializable]
public enum ForcefieldGeneratorVisual
{
    Active,
    Charge
}
