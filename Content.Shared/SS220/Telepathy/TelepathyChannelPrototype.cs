// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Telepathy;

[Prototype("telepathyChannel")]
public sealed partial class TelepathyChannelPrototype : IPrototype
{
    /// <summary>
    /// Human-readable name for the channel.
    /// </summary>
    [DataField("name")]
    public string Name { get; private set; } = string.Empty;

    [ViewVariables(VVAccess.ReadOnly)]
    public string LocalizedName => Loc.GetString(Name);


    [DataField("color")]
    public Color Color { get; private set; } = Color.Lime;

    [IdDataField, ViewVariables]
    public string ID { get; } = default!;
}
