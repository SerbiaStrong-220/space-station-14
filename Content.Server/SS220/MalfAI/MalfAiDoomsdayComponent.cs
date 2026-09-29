// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Numerics;
using Robust.Shared.Map;

namespace Content.Server.SS220.MalfAI;

[RegisterComponent]
public sealed partial class MalfAiDoomsdayComponent : Component
{
    [ViewVariables]
    public EntityUid Station;

    [ViewVariables]
    public EntityUid Core;

    [ViewVariables]
    public EntityUid MindId;

    [ViewVariables]
    public EntityUid Role;

    [ViewVariables]
    public string PreviousAlertLevel = string.Empty;

    [ViewVariables]
    public bool WeSetAlert;

    [ViewVariables]
    public MalfAiDoomsdayPhase Phase;

    [ViewVariables]
    public int WaveKilled;

    [ViewVariables]
    public float LastFxRadius;

    [ViewVariables]
    public bool WaveAnnounced;

    [ViewVariables]
    public float StationRadius;

    [ViewVariables]
    public float[] RingRadius = [];

    [ViewVariables]
    public HashSet<EntityUid> Hit = new();

    [ViewVariables]
    public EntityUid WaveEntity;

    [ViewVariables]
    public TimeSpan WaveStartTime;

    [ViewVariables]
    public float WaveSpeed;

    [ViewVariables]
    public float WaveMaxRadius;

    [ViewVariables]
    public MapId WaveMapId;

    [ViewVariables]
    public Vector2 WaveOrigin;
}

[RegisterComponent]
public sealed partial class MalfAiDoomsdayCoreComponent : Component
{
    [ViewVariables]
    public EntityUid Rule;
}

public enum MalfAiDoomsdayPhase : byte
{
    Armed,
    Cancelled,
    Wave,
    Completed
}
