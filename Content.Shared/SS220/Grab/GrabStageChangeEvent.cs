namespace Content.Shared.SS220.Grab;

public sealed partial class GrabStageChangeEvent : EntityEventArgs
{
    public EntityUid Grabber;
    public EntityUid Grabbable;
    public GrabStage NewStage;

    public GrabStageChangeEvent(EntityUid grabber, EntityUid grabbable, GrabStage newStage)
    {
        Grabber = grabber;
        Grabbable = grabbable;
        NewStage = newStage;
    }
}
