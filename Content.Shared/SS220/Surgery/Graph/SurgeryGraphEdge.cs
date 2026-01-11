// Original code from construction graph all edits under © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Surgery.Graph;

[Serializable]
[DataDefinition]
public sealed partial class SurgeryGraphEdge : ISerializationHooks
{
    [DataField("to", required: true)]
    public string Target = string.Empty;

    [DataField]
    public ProtoId<AbstractSurgeryEdgePrototype>? BaseEdge { get; private set; }

    [DataField("requirements")]
    private SurgeryGraphRequirement[] _requirements = Array.Empty<SurgeryGraphRequirement>();

    [DataField("visibilityRequirements")]
    private SurgeryGraphRequirement[] _visibilityRequirements = Array.Empty<SurgeryGraphRequirement>();

    [DataField("actions", serverOnly: true)]
    private ISurgeryGraphEdgeAction[] _actions = Array.Empty<ISurgeryGraphEdgeAction>();

    [DataField("actionDescription")]
    private LocId[] _actionLocIds = Array.Empty<LocId>();

    /// <summary>
    /// Time which this step takes in seconds
    /// </summary>
    [DataField]
    [Access(typeof(SurgeryGraphSystem))]
    public float? Delay { get; private set; }

    /// <summary>
    /// This sound will be played when graph gets to target node
    /// </summary>
    [DataField("sound")]
    [Access(typeof(SurgeryGraphSystem))]
    public SoundSpecifier? EndSound { get; private set; } = null;

    [ViewVariables]
    [Access(typeof(SurgeryGraphSystem))]
    public IReadOnlyList<SurgeryGraphRequirement> Requirements => _requirements;

    [ViewVariables]
    [Access(typeof(SurgeryGraphSystem))]
    public IReadOnlyList<SurgeryGraphRequirement> VisibilityRequirements => _visibilityRequirements;

    [ViewVariables]
    [Access(typeof(SurgeryGraphSystem))]
    public IReadOnlyList<ISurgeryGraphEdgeAction> Actions => _actions;

    [ViewVariables]
    [Access(typeof(SurgeryGraphSystem))]
    public IReadOnlyList<LocId> ActionLocIds => _actionLocIds;

    void ISerializationHooks.AfterDeserialization()
    {
        if (Delay == null && BaseEdge == null)
            throw new Exception($"Null delay found in edge targeted to {Target}");
    }
}
