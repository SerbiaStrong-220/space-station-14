using Robust.Shared.GameObjects;

namespace Content.Server.SS220.DarkForces.Narsi.Progress.Objectives.Rituals;

[ByRefEvent]
public record struct NarsiRitualCompletedEvent(string Ritual);
