// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Pathology;

namespace Content.Shared.SS220.Body;

/// <summary>
/// A body part that a strong cut or blow may tear off once it has a severe bone trauma.
/// Parts without it, e.g. the torso, are never torn off.
/// </summary>
[RegisterComponent]
public sealed partial class DismemberableComponent : Component
{
    /// <summary>
    /// Chance of a hit to tear this part off, used instead of <see cref="DismembermentComponent.HitRoll"/> of the body, e.g. for the head.
    /// </summary>
    [DataField]
    public TraumaRoll? HitRoll;

    /// <summary>
    /// An explosion may tear the part off even without a trauma.
    /// </summary>
    [DataField]
    public bool TornByBlast;
}
