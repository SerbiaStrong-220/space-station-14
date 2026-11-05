// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Grab;

namespace Content.Shared.SS220.MartialArts.Sequence.Effects;

public sealed partial class SetGrabStageCombatEffect : CombatSequenceEffect
{
    [DataField(required: true)]
    public GrabStage Stage;

    /// <summary>
    /// If target wasn't grabbed before, it will be at the end of the combo
    /// </summary>
    [DataField]
    public bool CreateGrab = true;

    public override void Execute(Entity<MartialArtistComponent> user, EntityUid target)
    {
        var grab = Entity.System<SharedGrabSystem>();

        if (!Entity.TryGetComponent<GrabberComponent>(user, out var grabber))
            return;

        if (!Entity.TryGetComponent<GrabbableComponent>(target, out var grabbable))
            return;

        if (!grab.IsGrabbed((target, grabbable)))
        {
            if (!CreateGrab)
                return;

            grab.DoInitialGrab((user, grabber), (target, grabbable), Stage);
            return;
        }

        grab.ChangeGrabStage((user, grabber), (target, grabbable), Stage);
    }
}
