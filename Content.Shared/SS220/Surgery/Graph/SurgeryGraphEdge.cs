// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Audio;

namespace Content.Shared.SS220.Surgery.Graph;

[Serializable]
[DataDefinition]
public sealed partial class SurgeryGraphEdge
{
    [DataField("conditions")]
    private ISurgeryGraphCondition[] _conditions = Array.Empty<ISurgeryGraphCondition>();

    [DataField("actions", serverOnly: true)]
    private ISurgeryGraphAction[] _action = Array.Empty<ISurgeryGraphAction>();

    [DataField("to", required: true)]
    public string Target { get; private set; } = string.Empty;

    /// <summary>
    /// Time which this step takes in seconds
    /// </summary>
    [DataField]
    public float Delay { get; private set; } = 4f;

    /// <summary>
    /// This sound will be played when graph gets to target node
    /// </summary>
    [DataField("sound")]
    public SoundSpecifier? EndSurgerySound { get; private set; } = null;

    [ViewVariables]
    public IReadOnlyList<ISurgeryGraphCondition> Conditions => _conditions;

    [ViewVariables]
    public IReadOnlyList<ISurgeryGraphAction> Action => _action;
}
