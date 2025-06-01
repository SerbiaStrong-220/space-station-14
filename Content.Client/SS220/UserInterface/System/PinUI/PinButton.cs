// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.Resources;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.SS220.UserInterface.System.PinUI;

public sealed class PinButton : TextureButton
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    private readonly PinUISystem _pinUISystem;

    public Control? AttachedControl;

    public PinButton()
    {
        IoCManager.InjectDependencies(this);

        _pinUISystem = _entityManager.System<PinUISystem>();
        TextureNormal = _resourceCache.GetTexture("/Textures/SS220/Interface/pin.png");
        ToggleMode = true;
        VerticalAlignment = VAlignment.Center;
        OnToggled += args => SetPinned(args.Pressed);
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();

        _pinUISystem.OnPinStateChanged += OnPinStateChanged;
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();

        SetPinned(false);
        _pinUISystem.OnPinStateChanged -= OnPinStateChanged;
    }

    private void OnPinStateChanged(PinStateChangedArgs args)
    {
        if (AttachedControl != args.Control)
            return;

        Pressed = args.Pinned;
        Modulate = args.Pinned ? Color.Green : Color.White;
    }

    private void SetPinned(bool pinned)
    {
        if (AttachedControl is { } control)
            _pinUISystem.SetPinned(control, Pressed);
    }

    public PinButton(Control attachedControl) : this()
    {
        AttachedControl = attachedControl;
    }

    public void AttachControl(Control control)
    {
        AttachedControl = control;
    }

    public void DeattachControl(bool unpin = true)
    {
        if (unpin)
            SetPinned(false);

        AttachedControl = null;
    }
}

