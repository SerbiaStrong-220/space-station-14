using Robust.Shared.Serialization;

namespace Content.Shared.SS220.RoundEndInfo;

/// <summary>
/// Base system for handling round-end information.
/// </summary>
public abstract partial class SharedRoundEndInfoSystem : EntitySystem;

[Serializable, NetSerializable]
public sealed class RoundEndInfoSendEvent: EntityEventArgs;

/// <summary>
/// Marker interface for round-end data types.
/// </summary>
public interface IRoundEndInfoData;

/// <summary>
/// Event sent at the end of the round containing additional round-end information blocks.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class RoundEndAdditionalInfoEvent : EntityEventArgs
{
    /// <summary>
    /// A list of additional round-end info data to be displayed.
    /// </summary>
    public List<IRoundEndInfoData> AdditionalInfo { get; set; } = new();
}

/// <summary>
/// A generic display block for round-end information with a title, body text, and color.
/// </summary>
[Serializable, NetSerializable]
public sealed class RoundEndInfoDisplayBlock : IRoundEndInfoData
{
    /// <summary>
    /// The title of the display block.
    /// </summary>
    public string Title = string.Empty;

    /// <summary>
    /// The body text of the display block.
    /// </summary>
    public string Body = string.Empty;

    /// <summary>
    /// The background color of the display block.
    /// </summary>
    public Color Color = new(30, 30, 30, 200);
}

/// <summary>
/// Contains data about antagonist purchases during the round.
/// </summary>
[Serializable, NetSerializable]
public sealed class RoundEndAntagPurchaseData : IRoundEndInfoData
{
    /// <summary>
    /// The name of the player.
    /// </summary>
    public string Name = string.Empty;

    /// <summary>
    /// A list of item prototype IDs purchased by the player.
    /// </summary>
    public List<string> ItemPrototypes = new();

    /// <summary>
    /// The total amount of TC spent.
    /// </summary>
    public int TotalTC;
}

/// <summary>
/// Event containing all antagonist purchase data for the round.
/// </summary>
[Serializable, NetSerializable]
public sealed class RoundEndAntagItemsEvent : EntityEventArgs
{
    /// <summary>
    /// A list of antagonist purchase data per player.
    /// </summary>
    public List<RoundEndAntagPurchaseData> PlayerPurchases = new();
}

/// <summary>
/// Tracks the amount of food consumed by a player.
/// </summary>
[Serializable, NetSerializable]
public sealed class FoodData
{
    /// <summary>
    /// Total number of food items consumed.
    /// </summary>
    public int AmountFood;
}

/// <summary>
/// Tracks cargo items purchased by a player.
/// </summary>
[Serializable, NetSerializable]
public sealed class CargoData
{
    /// <summary>
    /// A list of purchased items and their costs.
    /// </summary>
    public List<(string, int)> Items = new();
}

/// <summary>
/// Tracks the total number of shots fired by a player.
/// </summary>
[Serializable, NetSerializable]
public sealed class GunData
{
    /// <summary>
    /// Total number of shots fired.
    /// </summary>
    public int TotalShots;
}

/// <summary>
/// Tracks the number of puddles cleaned or created by a player.
/// </summary>
[Serializable, NetSerializable]
public sealed class PuddleData
{
    /// <summary>
    /// Total number of puddles interacted with.
    /// </summary>
    public int TotalPuddle;
}

/// <summary>
/// Tracks the times at which a player died during the round.
/// </summary>
[Serializable, NetSerializable]
public sealed class DeathData
{
    /// <summary>
    /// A list of timestamps representing each death.
    /// </summary>
    public List<TimeSpan> TimeOfDeath = new();
}
