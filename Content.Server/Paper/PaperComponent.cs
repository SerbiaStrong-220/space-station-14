using Content.Shared.Paper;
using Robust.Shared.GameStates;

namespace Content.Server.Paper;

[NetworkedComponent, RegisterComponent]
public sealed partial class PaperComponent : SharedPaperComponent
{
    public PaperAction Mode;
    [DataField("content")]
    public string Content { get; set; } = "";

    /// <summary>
    ///     Allows to forbid to write on paper without using stamps as a hack
    /// </summary>
    [DataField("writable")]
    public bool Writable { get; set; } = true;

    [DataField("contentSize")]
    public int ContentSize { get; set; } = 6000;

    [DataField("stampedBy")]
    public List<StampDisplayInfo> StampedBy { get; set; } = new();

    /// <summary>
    ///     Stamp to be displayed on the paper, state from bureaucracy.rsi
    /// </summary>
    [DataField("stampState")]
    public string? StampState { get; set; }
}
