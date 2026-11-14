// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
namespace Content.Server.SS220.MalfAI;

[RegisterComponent]
public sealed partial class MalfAiLockdownComponent : Component
{
    [ViewVariables]
    public EntityUid? Station;

    [ViewVariables]
    public HashSet<EntityUid> Bolted = new();

    [ViewVariables]
    public HashSet<EntityUid> Electrified = new();

    [ViewVariables]
    public HashSet<EntityUid> Closed = new();
}

[RegisterComponent]
public sealed partial class MalfAiLockdownRestoreComponent : Component
{
    [ViewVariables]
    public bool Unbolt;

    [ViewVariables]
    public bool Open;

    public bool Restoring;
}
