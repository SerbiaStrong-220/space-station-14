// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Virology;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class VirusOverloadComponent : Component
{
    /// <summary>How long after reaching 2+ viruses effect turns on.</summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromMinutes(5);

    /// <summary>When it turns on, slow and blur ramp over this time.</summary>
    [DataField]
    public TimeSpan RampDuration = TimeSpan.FromMinutes(1);

    /// <summary>Walk/run speed coefficient for 2 strains, and for 3+.</summary>
    [DataField]
    public float Slow2 = 0.85f;

    [DataField]
    public float Slow3 = 0.75f;

    /// <summary>Vision-blur strength for 2 strains, and for 3+.</summary>
    [DataField]
    public float Blind2 = 0.15f;

    [DataField]
    public float Blind3 = 0.35f;

    /// <summary>Current active movement coefficients.</summary>
    [DataField, AutoNetworkedField]
    public float Walk = 1f;

    [DataField, AutoNetworkedField]
    public float Sprint = 1f;

    public TimeSpan? ReachedAt;

    public TimeSpan? ActivatedAt;

    public int AppliedBlur;

    public bool Reverting;
}
