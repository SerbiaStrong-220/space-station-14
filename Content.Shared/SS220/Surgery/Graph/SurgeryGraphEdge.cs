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

    [DataField("conditions")]
    private ISurgeryGraphCondition[] _conditions = Array.Empty<ISurgeryGraphCondition>();

    [DataField("actions", serverOnly: true)]
    private ISurgeryGraphAction[] _actions = Array.Empty<ISurgeryGraphAction>();

    /// <summary>
    /// Time which this step takes in seconds
    /// </summary>
    [DataField]
    public float? Delay { get; private set; }

    /// <summary>
    /// This sound will be played when graph gets to target node
    /// </summary>
    [DataField("sound")]
    public SoundSpecifier? EndSound { get; private set; } = null;

    /// <summary>
    /// Don't know what u are doing? -> use <see cref="SurgeryGraphSystem"/>
    /// </summary>
    [ViewVariables]
    public IReadOnlyList<ISurgeryGraphCondition> Conditions => _conditions;

    /// <summary>
    /// Don't know what u are doing? -> use <see cref="SurgeryGraphSystem"/>
    /// </summary>
    [ViewVariables]
    public IReadOnlyList<ISurgeryGraphAction> Actions => _actions;

    void ISerializationHooks.AfterDeserialization()
    {
        if (Delay == null && BaseEdge == null)
            throw new Exception($"Null delay found in edge targeted to {Target}");
    }
}
