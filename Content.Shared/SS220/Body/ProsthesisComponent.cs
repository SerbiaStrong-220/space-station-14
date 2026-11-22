// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Body;

/// <summary>
/// An artificial organ: it can't get wounds, isn't rejected and is put on or taken off without opening the body.
/// </summary>
[RegisterComponent, Access(typeof(ProsthesisSystem))]
public sealed partial class ProsthesisComponent : Component
{
    /// <summary>
    /// Multiplies the body's movement speed while installed, e.g. a wooden leg is slower than a living one.
    /// </summary>
    [DataField]
    public float SpeedModifier = 1f;

    /// <summary>
    /// An EMP drops the item from the hand of this prosthesis and keeps the hand busy for this long.
    /// </summary>
    [DataField]
    public TimeSpan? EmpHandJamTime;

    /// <summary>
    /// An EMP knocks the body down for this long.
    /// </summary>
    [DataField]
    public TimeSpan? EmpKnockdownTime;

    /// <summary>
    /// Multiplies the chance of an explosion to tear the prosthesis off.
    /// </summary>
    [DataField]
    public float ExplosionDismemberModifier = 1f;
}
