// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.CultYogg;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedMiGoSystem), Friend = AccessPermissions.ReadWriteExecute, Other = AccessPermissions.Read)]
public sealed partial class MiGoComponent : Component
{
    /// ABILITIES ///
    [DataField]
    public EntProtoId MiGoEnslavementAction = "ActionMiGoEnslavement";

    [DataField]
    public EntProtoId MiGoHealAction = "ActionMiGoHeal";

    [DataField]
    public EntProtoId MiGoAstralAction = "ActionMiGoAstral";

    [DataField]
    public EntProtoId MiGoErectAction = "ActionMiGoErect";

    [DataField]
    public EntProtoId MiGoSacrificeAction = "ActionMiGoSacrifice";

    [DataField, AutoNetworkedField]
    public EntityUid? MiGoEnslavementActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? MiGoAstralActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? MiGoErectActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? MiGoSacrificeActionEntity;

    //Astral variables
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public bool PhysicalForm = true;//Is MiGo in phisycal form?

    [ViewVariables]
    public TimeSpan? MaterializedStart;

    [ViewVariables, DataField, AutoNetworkedField]
    public float MaterialMovementSpeed = 4f; //ToDo check this thing

    [ViewVariables, DataField, AutoNetworkedField]
    public float UnMaterialMovementSpeed = 1f;//ToDo check this thing
}

//Visual
[Serializable, NetSerializable]
public enum MiGoVisual
{
    Base
}

//UI for creation foundation of a building
[NetSerializable, Serializable]
public enum SiliconLawsUiKey : byte
{
    Key
}
