// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Body;

/// <summary>
/// A body part that a strong cut or blow may tear off once it has a severe trauma.
/// Parts without it, e.g. the torso, are never torn off.
/// </summary>
[RegisterComponent]
public sealed partial class DismemberableComponent : Component
{
    /// <summary>
    /// Cuts and blows of one hit needed to roll the tearing.
    /// </summary>
    [DataField]
    public float HitThreshold = 15f;

    /// <summary>
    /// Chance of a hit to tear the part off, used instead of the one growing with the damage, e.g. for the head.
    /// </summary>
    [DataField]
    public float? HitChance;

    /// <summary>
    /// An explosion may tear the part off even without a trauma.
    /// </summary>
    [DataField]
    public bool TornByBlast;
}
