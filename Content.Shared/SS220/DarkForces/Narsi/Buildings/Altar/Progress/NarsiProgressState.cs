using Robust.Shared.Serialization;

namespace Content.Shared.SS220.DarkForces.Narsi.Buildings.Altar.Progress;

[Serializable, NetSerializable]
public record NarsiProgressState(string BloodScore, string ObjectiveDescription);

