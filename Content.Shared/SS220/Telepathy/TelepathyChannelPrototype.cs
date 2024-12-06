// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Telepathy;

[Prototype("telepathyChannel")]
public sealed partial class TelepathyChannelPrototype : IPrototype
{
    [IdDataField, ViewVariables]
    public string ID { get; } = default!;

    [DataField]
    public ChannelParameters ChannelParameters = new();
}

[DataDefinition]
public sealed partial class ChannelParameters()
{
    [DataField("name")]
    public string Name { get; private set; } = string.Empty;

    [DataField("color")]
    public Color Color { get; private set; } = Color.Lime;

    public ChannelParameters(string name, Color color) : this()
    {
        Name = name;
        Color = color;
    }
}
