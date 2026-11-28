// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Body;

/// <summary>
/// An organ that keeps the state of the body it was taken from.
/// Put into another body, an organ of a zombie or of an infected body infects it and poisons it,
/// a rotten one poisons it if <see cref="PoisonsWhenRotten"/>.
/// </summary>
[RegisterComponent]
public sealed partial class TaintableOrganComponent : Component
{
    /// <summary>
    /// Inner organs fail by their own pathologies when rotten, only the body parts poison.
    /// </summary>
    [DataField]
    public bool PoisonsWhenRotten;

    [ViewVariables]
    public bool Zombified;

    [ViewVariables]
    public bool Rotten;
}
