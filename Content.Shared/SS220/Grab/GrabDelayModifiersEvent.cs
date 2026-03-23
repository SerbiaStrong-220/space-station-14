// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Grab;

public sealed partial class GrabDelayModifiersEvent
{
    public readonly EntityUid Grabber;
    public readonly EntityUid Grabbable;
    public readonly GrabStage Stage;
    public TimeSpan Delay { get; private set; }

    public GrabDelayModifiersEvent(EntityUid grabber, EntityUid grabbable, GrabStage stage, TimeSpan delay)
    {
        Grabber = grabber;
        Grabbable = grabbable;
        Stage = stage;
        Delay = delay;
    }

    public void Add(TimeSpan delay)
    {
        Delay += delay;
    }
    public void Multiply(float modifier)
    {
        Delay *= modifier;
    }
}
