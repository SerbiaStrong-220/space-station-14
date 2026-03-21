// Taken from: Corvax https://github.com/space-syndicate/space-station-14

using Robust.Shared.Prototypes;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.SS220.Ipc;

/// <summary>
/// Prototype defining a collection of IPC face sprites.
/// </summary>
[Prototype("ipcFaceProfile")]
public sealed partial class IpcFaceProfilePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("faces", required: true)]
    public List<IpcFaceEntry> Faces { get; private set; } = new();
}

[DataDefinition]
public sealed partial class IpcFaceEntry
{
    [DataField("id", required: true)]
    public ProtoId<MarkingPrototype> Id { get; private set; }

    [DataField("category")]
    public string Category { get; private set; } = "all"; 
}