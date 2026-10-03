namespace Content.Shared.SS220.RoundEndInfo;

public interface IBaseInfoState;

/// <summary>
/// Tracks the amount of food consumed by a player.
/// </summary>
public record struct FoodInfoState(int TotalFood) : IBaseInfoState;

/// <summary>
/// Tracks the total number of shots fired by a player.
/// </summary>
public record struct GunInfoState(int TotalShots) : IBaseInfoState;

/// <summary>
/// Tracks the number of puddles cleaned or created by a player.
/// </summary>
public record struct PuddleInfoState(int TotalPuddle) : IBaseInfoState;

/// <summary>
/// Tracks cargo items purchased by a player.
/// </summary>
public struct CargoInfoState() : IBaseInfoState
{
    public List<(string, int, int)>? Items { get; set; } = new();
}

/// <summary>
/// Tracks the times at which a player died during the round.
/// </summary>
public record struct DeathInfoState() : IBaseInfoState
{
    public List<TimeSpan>? TimeOfDeath { get; set; } = new();
}

public record struct EmergencyShuttleInfoState : IBaseInfoState
{
    public TimeSpan? FirstEmergencyCallTime { get; set; }
    public TimeSpan? LastEmergencyCallTime { get; set; }
    public int TotalCalled { get; set; }
}

public record struct HealingInfoState(int TotalHealed) : IBaseInfoState;

public record struct StunBatonInfoState(int TotalHits) : IBaseInfoState;
