// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Virology.Behaviors;


[RegisterComponent]
public sealed partial class VirusSchizophreniaComponent : Component
{
    /// <summary>Pool of self-chat lines this symptom picks from.</summary>
    [DataField]
    public ProtoId<VirusMessagePoolPrototype> Pool = "SchizophreniaThoughts";

    /// <summary>Random cd between messages.</summary>
    [DataField]
    public TimeSpan MinInterval = TimeSpan.FromSeconds(20);

    [DataField]
    public TimeSpan MaxInterval = TimeSpan.FromSeconds(55);

    [ViewVariables]
    public TimeSpan NextMessage;

    /// <summary>No same line picked twice in a row.</summary>
    [ViewVariables]
    public int LastIndex = -1;
}
