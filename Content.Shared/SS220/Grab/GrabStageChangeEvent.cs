// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Grab;

/// <summary>
/// Raised both on grabber and grabbable
/// </summary>
public sealed partial class GrabStageChangeEvent : EntityEventArgs
{
    public readonly EntityUid Grabber;
    public readonly EntityUid Grabbable;
    public readonly GrabStage OldStage;
    public readonly GrabStage NewStage;

    public GrabStageChangeEvent(EntityUid grabber, EntityUid grabbable, GrabStage oldStage, GrabStage newStage)
    {
        Grabber = grabber;
        Grabbable = grabbable;
        OldStage = oldStage;
        NewStage = newStage;
    }
}
