using Robust.Shared.Serialization;

namespace Content.Shared.SS220.RoundEndInfo;

/// <summary>
/// Base system for handling round-end information.
/// </summary>
public abstract partial class SharedRoundEndInfoSystem : EntitySystem;

/// <summary>
/// Marker interface for round-end data types.
/// </summary>
public interface IRoundEndInfoData;

/// <summary>
/// Event used to determine whether a player should be included when recording additional round-end information.
/// </summary>
/// <remarks>
/// This event is raised locally on the player's mind before adding a new round-end entry.
/// Any system can handle this event and set <see cref="Cancelled"/> to <c>true</c>
/// to exclude the player from being counted in round-end statistics (e.g., if they are in an admin arena).
/// </remarks>
[ByRefEvent]
public record struct RoundEndAdditionalInfoCheckMapEvent(EntityUid? User, bool Cancelled = false);

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
