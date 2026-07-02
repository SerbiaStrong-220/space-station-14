// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Pathology;

[RegisterComponent]
public sealed partial class HypersomniaComponent : Component
{
    /// <summary>The symptom this reads the current stage from.</summary>
    [DataField]
    public ProtoId<PathologyPrototype> Pathology = "Hypersomnia";

    /// <summary>How long each forced-sleep lasts.</summary>
    [DataField]
    public TimeSpan SleepDuration = TimeSpan.FromSeconds(30);

    /// <summary>Grace period so host actually have time to wake up and stand</summary>
    [DataField]
    public TimeSpan WakeGrace = TimeSpan.FromSeconds(30);

    /// <summary>Minimum gap between sleep episodes (stage 3).</summary>
    [DataField]
    public TimeSpan MinInterval = TimeSpan.FromMinutes(1);

    /// <summary>Maximum gap between sleep episodes (stage 3).</summary>
    [DataField]
    public TimeSpan MaxInterval = TimeSpan.FromMinutes(3);

    /// <summary>Stage 3: when the next episode is due. Null until stage 3 is reached.</summary>
    [ViewVariables]
    public TimeSpan? NextSleep;
}
