using Content.Shared.Dataset;
using Robust.Shared.Prototypes;

namespace Content.Shared.Players.PlayTimeTracking;

public static class PlayTimeTrackingShared
{
    /// <summary>
    /// The prototype ID of the play time tracker that represents overall playtime, i.e. not tied to any one role.
    /// </summary>
    public static readonly ProtoId<PlayTimeTrackerPrototype> TrackerOverall = "Overall";

    // // <summary>
    // // The prototype ID of the play time tracker that represents overall time with admin priveleges.
    // // </summary>
    // public static readonly ProtoId<PlayTimeTrackerPrototype> TrackerAdmin = "Admin";

    //SS220-aghost-playtime begin
    /// <summary>
    /// The prototype ID of the play time tracker that represents overall time with admin priveleges.
    /// </summary>
    public static readonly ProtoId<PlayTimeTrackerPrototype> TrackerAdmin = "AdminTime";

    /// <summary>
    /// The prototype ID of the play time tracker that represents admin ghost playtime.
    /// </summary>
    public static readonly ProtoId<PlayTimeTrackerPrototype> TrackerAGhost = "AGhostTime";

    /// <summary>
    /// The prototype ID of the play time tracker that represents overall time in ghost.
    /// </summary>
    public static readonly ProtoId<PlayTimeTrackerPrototype> TrackerObserver = "ObserverTime";
    //SS220-aghost-playtime end

}
