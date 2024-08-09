// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Content.Shared.SS220.CultYogg.EntitySystems;

namespace Content.Shared.SS220.CultYogg.Components;

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
    public EntityUid? MiGoHealActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? MiGoAstralActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? MiGoErectActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? MiGoSacrificeActionEntity;

    //Enlsavement requirements
    public string RequiedEffect = "Rave";

    //How long heal effect will occure
    public float HealingEffectTime = 100;

    //Astral variables
    [ViewVariables, AutoNetworkedField]
    public bool PhysicalForm = true;//Is MiGo in phisycal form?

    public TimeSpan CooldownAfterMaterialize = TimeSpan.FromSeconds(3);

    /// <summary>
    /// How long reaper can stay materialized, depending on stage
    /// </summary>
    [ViewVariables, DataField, AutoNetworkedField]
    public List<TimeSpan> MaterializeDurations = new()
    {
        TimeSpan.FromSeconds(15),
        TimeSpan.FromSeconds(20),
        TimeSpan.FromSeconds(40)
    };


    [ViewVariables]
    public TimeSpan? DeMaterializedStart;

    [ViewVariables, DataField, AutoNetworkedField]
    public float MaterialMovementSpeed = 6f; //ToDo check this thing

    [ViewVariables, DataField, AutoNetworkedField]
    public float UnMaterialMovementSpeed = 18f;//ToDo check this thing
}

//Visual
[Serializable, NetSerializable]
public enum MiGoVisual
{
    Base
}

//UI for creation foundation of a building
[NetSerializable, Serializable]
public enum MiGoErectUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class MiGoErectBuiState : BoundUserInterfaceState
{
    public MiGoErectBuiState()
    {
    }
}
