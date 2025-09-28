// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.PinnableUI;
using Content.Shared.UserInterface;
using Content.Shared.Verbs;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.SS220.UserInterface.System.PinUI;

public sealed class PinUISystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public Action<PinStateChangedArgs>? OnPinStateChanged;

    private readonly HashSet<Control> _pinnedControls = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PinnableUIComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    private void OnGetVerbs(Entity<PinnableUIComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !TryComp<ActivatableUIComponent>(ent, out var activatable))
            return;

        if (activatable.Key == null)
            return;

        if (!_ui.IsUiOpen(ent.Owner, activatable.Key))
            return;

        if (!_gameTiming.IsFirstTimePredicted)
            return;

        var verb = new Verb
        {
            Act = () =>
            {
                _ui.SetUiState(ent.Owner, activatable.Key, new PinControlState());
            },
            Text = Loc.GetString("verb-pin-ui"),
            Icon = ent.Comp.Icon,
        };

        args.Verbs.Add(verb);
    }

    public static TextureButton AddPinButtonBeforeTarget(Control linkedControl, Control target)
    {
        var button = new PinButton(linkedControl);
        var parent = target.Parent;
        if (parent == null)
            return button;

        var index = target.GetPositionInParent();
        parent.AddChild(button);
        button.SetPositionInParent(index);

        return button;
    }

    public void SetPinned(Control control, bool pinned)
    {
        if (pinned && _pinnedControls.Add(control))
            OnPinStateChanged?.Invoke(new PinStateChangedArgs(control, true));
        else if (_pinnedControls.Remove(control))
            OnPinStateChanged?.Invoke(new PinStateChangedArgs(control, false));
    }

    public void SetPinned(Control? control)
    {
        if (control == null)
            return;

        if (_pinnedControls.Add(control))
            OnPinStateChanged?.Invoke(new PinStateChangedArgs(control, true));
        else if (_pinnedControls.Remove(control))
            OnPinStateChanged?.Invoke(new PinStateChangedArgs(control, false));
    }

    public bool IsPinned(Control control)
    {
        return _pinnedControls.Contains(control);
    }
}

public record struct PinStateChangedArgs(Control Control, bool Pinned);

public sealed partial class PinControlState : BoundUserInterfaceState;
