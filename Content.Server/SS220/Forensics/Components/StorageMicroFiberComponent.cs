namespace Content.Server.SS220.Forensics.Components;

/// <summary>
/// Persistent identity of material transferred from clothing and storage onto contained items.
/// </summary>
[RegisterComponent]
public sealed partial class StorageMicroFiberComponent : Component
{
    /// <summary>
    /// Random identifier generated once for this item and retained when its deposited evidence is cleaned.
    /// </summary>
    [DataField]
    public string? Code;
}
