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
    public List<(string, int, int)> Items { get; } = new();
}

/// <summary>
/// Tracks the times at which a player died during the round.
/// </summary>
public record struct DeathInfoState() : IBaseInfoState
{
    public List<TimeSpan> TimeOfDeath { get; } = new();
}

