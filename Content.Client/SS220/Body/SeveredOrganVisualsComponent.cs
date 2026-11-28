// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Client.SS220.Body;

/// <summary>
/// An external organ that outside the body is drawn the way it looked on the body: skin color, markings and the organs attached to it,
/// e.g. a severed head keeps its hair and eyes.
/// </summary>
[RegisterComponent]
public sealed partial class SeveredOrganVisualsComponent : Component
{
    /// <summary>
    /// Sprite layers added on top of the organ's own one, removed on every redraw.
    /// </summary>
    [ViewVariables]
    public List<string> AddedLayers = new();
}
