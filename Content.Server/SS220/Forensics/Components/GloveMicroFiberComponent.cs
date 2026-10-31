// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Server.SS220.Forensics.Components;

/// <summary>
/// Persistent identity of a pair of gloves, separate from removable evidence on its surface.
/// This identity is sampled by the dedicated glove-micro-fiber scanner.
/// </summary>
[RegisterComponent]
public sealed partial class GloveMicroFiberComponent : Component
{
    /// <summary>
    /// Whether the gloves leave an identifiable trace and can be sampled by the scanner.
    /// </summary>
    [DataField]
    public bool Enabled = true;

    /// <summary>
    /// Random identifier generated once for this pair and retained when its fingertips are cut off.
    /// </summary>
    [DataField]
    public string? Code;
}
