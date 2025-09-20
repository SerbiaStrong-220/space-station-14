namespace Content.Shared.Roles;

/// <summary>
/// Raised directed on an entity when all spawning and post spawn procedures are done
/// </summary>
[ByRefEvent]
public record struct PlayerMobSpawnedAndProcessedEvent() { }
