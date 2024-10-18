using Content.Client.Message;
using Content.Client.Stylesheets;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.SS220.CultYogg.SedativeSting.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.SS220.CultYogg.SedativeSting.Systems;

public sealed class SedativeStingStatusControl : Control
{
    private readonly Entity<SedativeStingComponent> _parent;
    private readonly RichTextLabel _label;
    private readonly SharedSolutionContainerSystem _solutionContainers;

    private FixedPoint2 _prevVolume;
    private FixedPoint2 _prevMaxVolume;

    public SedativeStingStatusControl(Entity<SedativeStingComponent> parent, SharedSolutionContainerSystem solutionContainers)
    {
        _parent = parent;
        _solutionContainers = solutionContainers;
        _label = new RichTextLabel { StyleClasses = { StyleNano.StyleClassItemStatus } };
        AddChild(_label);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!_solutionContainers.TryGetSolution(_parent.Owner, _parent.Comp.SolutionName, out _, out var solution))
            return;

        // only updates the UI if any of the details are different than they previously were
        if (_prevVolume == solution.Volume
            && _prevMaxVolume == solution.MaxVolume)
            return;

        _prevVolume = solution.Volume;
        _prevMaxVolume = solution.MaxVolume;


        _label.SetMarkup(Loc.GetString("sedative-sting-volume-label",
            ("currentVolume", solution.Volume),
            ("totalVolume", solution.MaxVolume)));
    }
}
