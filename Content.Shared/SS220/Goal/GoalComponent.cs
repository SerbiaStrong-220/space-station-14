// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Audio;
using Content.Shared.Dataset;

namespace Content.Shared.SS220.Goal;
/// <summary>
/// Action raised to scream smth
/// </summary>
[RegisterComponent]
[AutoGenerateComponentState]
public sealed partial class GoalComponent : Component
{
    [DataField("goalAction", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    [AutoNetworkedField]
    public string GoalAction = "ActionGoal";

    [DataField("golActionEntity")]
    [AutoNetworkedField]
    public EntityUid? GoalActionEntity;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier GoalSound = new SoundPathSpecifier("/Audio/SS220/Voice/Goal/gooool.ogg");

    [DataField]
    public ProtoId<DatasetPrototype> GoalPhrases = "goalPhrases";
}
public sealed partial class GoalActionEvent : InstantActionEvent
{
    /// <summary>
    /// Sound played when toggling
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? GoalSound = new SoundPathSpecifier("/Audio/SS220/Voice/Goal/gooool.ogg");

    [DataField]
    public ProtoId<DatasetPrototype>? GoalPhrases;
}
