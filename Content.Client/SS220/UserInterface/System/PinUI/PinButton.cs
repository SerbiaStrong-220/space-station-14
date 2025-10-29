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

    private static readonly Color DefaultColor = Color.FromHex("#4B596A");
    private static readonly Color PressedColor = Color.Green;

    public Control? LinkedControl
    {
        get => _linkedControl;
        set => SetLinkedControl(value);
    }
    private Control? _linkedControl;

    public PinButton()
    {
        IoCManager.InjectDependencies(this);

        _pinUISystem = _entityManager.System<PinUISystem>();
        TextureNormal = _resourceCache.GetTexture("/Textures/SS220/Interface/Misc/pin.png");
        Modulate = DefaultColor;
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
        if (LinkedControl != args.Control)
            return;

        Pressed = args.Pinned;
        Modulate = args.Pinned ? PressedColor : DefaultColor;
    }

    private void SetPinned(bool pinned)
    {
        if (LinkedControl is { } control)
            _pinUISystem.SetPinned(control, pinned);
    }

    public PinButton(Control attachedControl) : this()
    {
        SetLinkedControl(attachedControl);
    }

    public void SetLinkedControl(Control? control, bool unpinCurrent = true)
    {
        if (unpinCurrent)
            SetPinned(false);

        _linkedControl = control;
    }
}

