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

    /// <summary>
    /// Single-character prefix to determine what channel a message should be sent to.
    /// </summary>
    [DataField("keycode")]
    public char KeyCode { get; private set; } = '\0';

    [DataField("color")]
    public Color Color { get; private set; } = Color.Lime;

    [IdDataField, ViewVariables]
    public string ID { get; } = default!;
}
