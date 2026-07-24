// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Virology.Behaviors;

[RegisterComponent]
public sealed partial class VirusHypersomniaComponent : Component
{
    /// <summary>Condition - falls asleep anywhere on a timer.</summary>
    [DataField]
    public bool Anywhere;

    [DataField]
    public TimeSpan SleepDuration = TimeSpan.FromSeconds(10);

    /// <summary>Extra grace after waking before next powernap.</summary>
    [DataField]
    public TimeSpan WakeGrace = TimeSpan.FromSeconds(15);

    [DataField]
    public TimeSpan MinInterval = TimeSpan.FromSeconds(45);

    [DataField]
    public TimeSpan MaxInterval = TimeSpan.FromSeconds(120);

    [ViewVariables]
    public TimeSpan? NextSleep;
}
