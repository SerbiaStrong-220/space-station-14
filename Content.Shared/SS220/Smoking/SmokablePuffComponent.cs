// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Atmos;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;

namespace Content.Shared.SS220.Smoking;

/// <summary>
/// Allows a smokable to be puffed via an action.
/// Each puff consumes part of the smokable solution, adds it to the user's bloodstream
/// and releases vapor gas mixture into the surrounding environment.
/// Entity must be lit and worn in the mask slot.
/// </summary>
[RegisterComponent]
public sealed partial class SmokablePuffComponent : Component
{
    /// <summary>
    /// Amount of solution taken from the smokable per puff.
    /// </summary>
    [DataField]
    public FixedPoint2 PuffCost = 15;

    /// <summary>
    /// Gas released into the environment on each puff.
    /// </summary>
    [DataField]
    public Gas GasType = Gas.WaterVapor;

    /// <summary>
    /// Amount of <see cref="GasType"/>, in moles, released on each puff.
    /// </summary>
    [DataField]
    public float Moles = 4f;

    /// <summary>
    /// Temperature of the released gas mixture, in Kelvin.
    /// </summary>
    [DataField]
    public float Temperature = 350f;

    /// <summary>
    /// How long it takes to inhale a puff.
    /// Also included in the action use delay.
    /// </summary>
    [DataField]
    public TimeSpan PuffDelay = TimeSpan.FromSeconds(1.2);

    /// <summary>
    /// Sound played when the user starts inhaling.
    /// </summary>
    [DataField]
    public SoundSpecifier? PuffInhaleSound = new SoundPathSpecifier("/Audio/SS220/Effects/puff.ogg");

    /// <summary>
    /// Time after a successful puff during which the next puff is not allowed.
    /// </summary>
    [DataField]
    public TimeSpan PuffCooldown = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Runtime timestamp before which the next puff is not allowed.
    /// Set by the server after each successful puff.
    /// </summary>
    public TimeSpan NextPuffTime = TimeSpan.Zero;
}
