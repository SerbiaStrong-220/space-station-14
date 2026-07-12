using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Felinid.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class FelinidHeavyGunRecoilComponent : Component
{
    [DataField]
    public float DistanceModifier = 1f;

    [DataField]
    public float DurationModifier = 1f;

    [DataField]
    public float KnockdownChanceModifier = 1f;

    [DataField]
    public float InitialVelocityFraction = 0.35f;

    [AutoNetworkedField]
    public bool RecoilActive;

    [AutoNetworkedField]
    public Vector2 RecoilDirection;

    [AutoNetworkedField]
    public float RecoilDistance;

    [AutoNetworkedField]
    public TimeSpan RecoilStartedAt;

    [AutoNetworkedField]
    public TimeSpan RecoilEndsAt;

    [AutoNetworkedField]
    public EntityUid? RecoilMapUid;
}
