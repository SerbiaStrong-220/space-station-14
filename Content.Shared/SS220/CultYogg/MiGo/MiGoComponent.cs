// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.CultYogg.MiGo;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedMiGoSystem), Friend = AccessPermissions.ReadWriteExecute, Other = AccessPermissions.Read)]
public sealed partial class MiGoComponent : Component
{
    #region Abilities
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
    public EntityUid? MiGoPlantActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? MiGoSacrificeActionEntity;
    #endregion

    /// <summary>
    ///Enlsavement variables
    /// <summary>
    public string RequiedEffect = "Rave";//Required effect for enslavement

    [DataField]
    public SoundSpecifier? EnslavingSound = new SoundPathSpecifier("/Audio/SS220/CultYogg/migo_slave.ogg");

    /// <summary>
    /// The time it takes to enslave the target
    /// </summary>
    [DataField]
    public TimeSpan EnslaveTime = TimeSpan.FromSeconds(3);

    /// <summary>
    ///Erect variables
    /// <summary>
    public TimeSpan HealingEffectTime = TimeSpan.FromSeconds(15);//How long heal effect will occure

    /// <summary>
    ///Erect variables
    /// <summary>
    [ViewVariables, DataField]
    public float ErectDoAfterSeconds = 3f;

    /// <summary>
    /// Base time to erase buildings.
    /// It is used if the entity doesn't <see cref="CultYogg.Buildings.CultYoggBuildingComponent"/> or <see cref="CultYogg.Buildings.CultYoggBuildingFrameComponent"/>
    /// </summary>
    [DataField]
    public TimeSpan BaseEraseTime = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Which entities can be erased by MiGo
    /// </summary>
    [DataField]
    public EntityWhitelist? EraseWhitelist = new()
    {
        Components =
        [
            "CultYoggBuilding",
            "CultYoggBuildingFrame"
        ]
    };
    #region Astral
    /// <summary>
    ///Astral variables
    /// <summary>
    [ViewVariables, AutoNetworkedField]
    public bool IsPhysicalForm = true;//Is MiGo in phisycal form?

    public bool AudioPlayed = false; //it should be played once in timer, but this shit being called several times somehow

    [DataField]
    public SoundSpecifier? SoundMaterialize = new SoundPathSpecifier("/Audio/SS220/CultYogg/migo_astral_out.ogg");

    [DataField]
    public SoundSpecifier? SoundDeMaterialize = new SoundPathSpecifier("/Audio/SS220/CultYogg/migo_astral_in.ogg");

    [DataField]
    public TimeSpan EnteringAstralDoAfter = TimeSpan.FromSeconds(2.8);//same lenght as sound

    [DataField]
    public TimeSpan ExitingAstralDoAfter = TimeSpan.FromSeconds(1);

    public TimeSpan CooldownAfterDematerialize = TimeSpan.FromSeconds(3);

    /// How long MiGo can be in astral
    [DataField, AutoNetworkedField]
    public TimeSpan AstralDuration = TimeSpan.FromSeconds(15);

    [AutoNetworkedField]
    public TimeSpan? MaterializationTime;

    [AutoNetworkedField]
    public FixedPoint2 AlertTime;

    [ViewVariables, DataField, AutoNetworkedField]
    public float MaterialMovementSpeed = 6f; //ToDo check this thing

    [ViewVariables, DataField, AutoNetworkedField]
    public float UnMaterialMovementSpeed = 18f;//ToDo check this thing

    [DataField]
    public ProtoId<AlertPrototype> AstralAlert = "MiGoAstralAlert";
    #endregion

    #region Replacement
    /// <summary>
    ///Replacement required cause MiGo is key character among
    /// <summary>

    //Marking if entity can be gibbed and replaced
    public bool MayBeReplaced = false;

    //Should the timer count down the time
    public bool ShouldBeCounted = false;

    /// <summary>
    /// How long it takes to be able to replace this migo
    /// </summary>
    public TimeSpan BeforeReplacementCooldown = TimeSpan.FromSeconds(300);

    /// <summary>
    /// Buffer to markup when time has come
    /// </summary>
    [DataField]
    public TimeSpan? ReplacementEventTime;
    #endregion
}

[NetSerializable, Serializable]
public enum MiGoTimerVisualLayers : byte
{
    Digit1,
    Digit2
}
[Serializable, NetSerializable]
public enum MiGoVisual
{
    Base,
    Astral
}
