// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Roles;
using Content.Shared.SS220.CultYogg.Cultists;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.CultYogg.MiGo;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MiGoComponent : Component
{
    #region Abilities
    /// ABILITIES ///
    [DataField]
    public EntProtoId MiGoToggleLightAction = "ActionMiGoToggleLight";

    [DataField]
    public EntProtoId MiGoEnslavementAction = "ActionMiGoEnslavement";

    [DataField]
    public EntProtoId MiGoHealAction = "ActionMiGoHeal";

    [DataField]
    public EntProtoId MiGoErectAction = "ActionMiGoErect";

    [DataField]
    public EntProtoId MiGoSacrificeAction = "ActionMiGoSacrifice";

    [DataField, AutoNetworkedField]
    public EntityUid? MiGoToggleLightActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? MiGoEnslavementActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? MiGoHealActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? MiGoErectActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? MiGoPlantActionEntity;

    [DataField, AutoNetworkedField]
    public EntityUid? MiGoSacrificeActionEntity;
    #endregion

    /// <summary>
    /// The effect necessary for enslavement
    /// </summary>
    [ViewVariables]
    public string RequiedEffect = "Rave";

    [DataField]
    public SoundSpecifier? EnslavingSound = new SoundPathSpecifier("/Audio/SS220/CultYogg/migo_slave.ogg");

    /// <summary>
    /// The time it takes to enslave the target
    /// </summary>
    [ViewVariables]
    public TimeSpan EnslaveTime = TimeSpan.FromSeconds(3);

    /// <summary>
    /// How long healing effect will occure
    /// </summary>
    [ViewVariables]
    public TimeSpan HealingEffectTime = TimeSpan.FromSeconds(15);

    /// <summary>
    /// How far from altar MiGo can start action
    /// </summary>
    [ViewVariables]
    public float SacrificeStartRange = 2f;

    #region Building
    /// <summary>
    /// How long does it take to erect a building
    /// </summary>
    [ViewVariables, DataField]
    public TimeSpan ErectDoAfterSeconds = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Base time to erase buildings.
    /// It is used if the entity doesn't <see cref="CultYogg.Buildings.CultYoggBuildingComponent"/> or <see cref="CultYogg.Buildings.CultYoggBuildingFrameComponent"/>
    /// </summary>
    [DataField]
    public TimeSpan BaseEraseTime = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Which entities can be erased by MiGo
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist? EraseWhitelist = new();

    /// <summary>
    /// How long capturing DoAfter will occure
    /// <summary>
    [ViewVariables]
    public TimeSpan CaptureDoAfterTime = TimeSpan.FromSeconds(5);

    /// <summary>
    /// List of capruring results
    /// <summary>
    public Dictionary<string, TimeSpan> CaptureCooldowns = [];
    #endregion

    /// <summary>
    /// Added job
    /// </summary>
    [ViewVariables]
    public ProtoId<JobPrototype> JobName = "MiGoJob";

    /// <summary>
    /// Progression stage
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public CultYoggStage CurrentStage = CultYoggStage.Initial;
}
