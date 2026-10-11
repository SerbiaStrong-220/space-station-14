namespace Content.Shared.SS220.Felinid;

/// <summary>
/// Raised after an eligible felinid receives weapon recoil.
/// </summary>
[ByRefEvent]
public readonly record struct GunRecoilImpulseEvent(float KnockdownChance, TimeSpan KnockdownTime);
