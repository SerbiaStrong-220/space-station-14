// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Forensics;

namespace Content.Server.SS220.Forensics.Components;

/// <summary>
/// Stores a single collected material sample, independent of the source's later state.
/// </summary>
[RegisterComponent]
public sealed partial class GloveMicroFiberSwabComponent : Component
{
    /// <summary>
    /// Whether this swab contains a collected sample, including a sample without identifiable material evidence.
    /// </summary>
    [DataField]
    public bool Used;

    /// <summary>
    /// The scanned item's name when this sample was collected, independent of later changes to the item.
    /// </summary>
    [DataField]
    public string SourceName = string.Empty;

    /// <summary>
    /// The glove traces captured when the sample was taken.
    /// </summary>
    [DataField]
    public List<GloveMicroFiberTrace> GloveMicroFibers = [];

    /// <summary>
    /// The individual material profiles captured when the sample was taken, independent of later source changes.
    /// </summary>
    [DataField]
    public List<StorageMicroFiberTrace> StorageMicroFibers = [];
}
