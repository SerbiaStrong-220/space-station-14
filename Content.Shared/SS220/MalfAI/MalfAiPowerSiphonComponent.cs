// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.MalfAI;

[RegisterComponent]
public sealed partial class MalfAiPowerSiphonTuningComponent : Component
{
    [DataField]
    public float InitialDraw = 50_000f;

    [DataField]
    public float RampPerSecond = 25_000f;

    [DataField]
    public float MaxDraw = 10_000_000f;

    [DataField]
    public float StallMinDraw = 200_000f;

    [DataField]
    public float StallRatio = 0.25f;

    [DataField]
    public float StallTimeoutSeconds = 10f;
}

[RegisterComponent]
public sealed partial class MalfAiPowerSiphonComponent : Component
{
    [ViewVariables]
    public float InitialDraw;

    [ViewVariables]
    public float RampPerSecond;

    [ViewVariables]
    public float MaxDraw;

    [ViewVariables]
    public float StallMinDraw;

    [ViewVariables]
    public float StallRatio;

    [ViewVariables]
    public float StallTimeoutSeconds;

    [ViewVariables]
    public float StallSeconds;

    [ViewVariables]
    public bool AddedTimedSpawner;

    [ViewVariables]
    public bool AddedNodeContainer;

    [ViewVariables]
    public string? AddedHvNodeId;

    [ViewVariables]
    public bool AddedPowerConsumer;
}
