// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;
using System.Numerics;

namespace Content.Shared.SS220.Movement.Components;

/// <summary>
///     Exists for use as a status effect.
///     Entity with this component will be funny to waddle.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TemporaryWaddleComponent : Component
{
    ///<summary>
    /// How high should they hop during the waddle? Higher hop = more energy.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Vector2 HopIntensity = new(0, 0.125f);

    /// <summary>
    /// How far should they rock backward and forward during the waddle?
    /// Each step will alternate between this being a positive and negative rotation. More rock = more scary.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float TumbleIntensity = 10.0f;

    /// <summary>
    /// How long should a complete step take? Less time = more chaos.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float AnimationLength = 0.66f;

    /// <summary>
    /// How much shorter should the animation be when running?
    /// </summary>
    [DataField, AutoNetworkedField]
    public float RunAnimationLengthMultiplier = 0.568f;
}
