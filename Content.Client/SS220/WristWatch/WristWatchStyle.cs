// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Numerics;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.WristWatch;

[Prototype("wristWatchStyle")]
public sealed partial class WristWatchStylePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;
    [DataField]
    public string PanelTexture = default!;
    [DataField]
    public Vector2 PanelSize;
    [DataField]
    public WristWatchLabelStyle HoursLabel;
    [DataField]
    public WristWatchLabelStyle MinutesLabel;
    [DataField]
    public WristWatchLabelStyle? SecondsLabel;
    [DataField]
    public WristWatchLabelStyle? FirstSeparator;
    [DataField]
    public WristWatchLabelStyle? SecondSeparator;
}

[DataDefinition]
public partial struct WristWatchLabelStyle
{
    [DataField]
    public FontStyle? Font;
    [DataField]
    public Color Color;
    [DataField]
    public Vector2 Position;
    [DataField]
    public Vector2 Size;
}

[DataDefinition]
public partial struct FontStyle
{
    [DataField]
    public string Path;
    [DataField]
    public int Size;
}
