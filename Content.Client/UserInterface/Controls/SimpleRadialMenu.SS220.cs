using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Controls;

public sealed partial class SimpleRadialMenu : RadialMenu
{
    private Tooltip CreateRichTooltip(Control hovered)
    {
        var tooltip = new Tooltip()
        {
            Tracking = hovered.TrackingTooltip,
        };

        if (FormattedMessage.TryFromMarkup(hovered.ToolTip ?? "", out var message))
        {
            tooltip.SetMessage(message);
        }
        else
        {
            tooltip.Text = hovered.ToolTip;
        }

        return tooltip;
    }

    #region virtual pointer

    // SS220-emote-wheel-rework-begin

    /// <summary>
    /// Maps each button to the model it was built from, so a pointer selection can be committed without
    /// going through the engine's click handling, and so the hub can name what is selected.
    /// </summary>
    private readonly Dictionary<RadialMenuButton, RadialMenuOptionBase> _buttonModels = new();

    private bool _pointerMode;
    private Vector2 _pointer;
    private int _outerAreaButtonIndex;
    private Label? _hubLabel;
    private PageDots? _pageDots;

    /// <summary>
    /// Vertical space reserved above and below the ring for the banner and slot indicator. Added
    /// symmetrically so the wheel itself stays centred in the window, and therefore on screen.
    /// </summary>
    private const float BandReserve = 44f;

    private Control? _banner;

    /// <summary>
    /// Pages of buttons. Only one is visible at a time; the mouse wheel moves between them.
    /// </summary>
    private readonly List<RadialContainer> _pages = new();

    private int _pageIndex;
    private bool _pointerSelectionDirty;

    /// <summary>
    /// True while the menu is driven by the virtual pointer rather than by the real cursor.
    /// </summary>
    public bool PointerMode => _pointerMode;

    /// <summary>
    /// Button the virtual pointer currently rests on, or null while it sits in the dead zone.
    /// </summary>
    public RadialMenuButton? PointerSelection { get; private set; }

    /// <summary>
    /// Sizes the window to the ring and creates the hub label. Called after the buttons are built,
    /// because both depend on the resolved settings.
    /// </summary>
    private void ApplyChrome(SimpleRadialMenuSettings settings)
    {
        // The window must be large enough to contain the ring: it is centred on screen using its own
        // size, so a window smaller than what it draws would put the wheel off-centre.
        if (settings.FixedOuterRadius is { } outer)
        {
            var diameter = (outer + settings.HighlightExpansion) * 2f;

            // Reserved above and below the ring, symmetrically so it stays in the middle of the window
            // and therefore on screen. Holds the slot dots below and the banner above.
            MinSize = new Vector2(diameter, diameter + BandReserve * 2f);
        }

        // Paged menus need the wheel event, which only reaches the menu if it stops ignoring mouse input.
        if (_pages.Count > 1)
            MouseFilter = MouseFilterMode.Pass;

        if (settings.ShowHubLabel && _hubLabel == null)
        {
            _hubLabel = new Label
            {
                Align = Label.AlignMode.Center,
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Center,
                // SetWidth, not MaxWidth: Label.MeasureOverride reports zero width whenever ClipText is
                // on, which for a centred child means it is arranged at zero width and never appears.
                SetWidth = (settings.FixedInnerRadius ?? 64f) * 2f,
                ClipText = true,
            };

            AddChrome(_hubLabel);
        }

        if (_pages.Count <= 1 || _pageDots != null)
            return;

        // Sits in the reserved band under the ring rather than in the hub, which keeps the hub free for
        // the emote name and makes the slot count readable at a glance.
        _pageDots = new PageDots
        {
            Count = _pages.Count,
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Bottom,
        };

        AddChrome(_pageDots);
        UpdatePageIndicator();
    }

    /// <summary>
    /// Places a control in the reserved band above the ring. Only shown while the menu is click-driven:
    /// during a hold it cannot be clicked anyway, and it would be noise while the player is aiming.
    /// </summary>
    public void SetBanner(Control control)
    {
        if (_banner != null)
            return;

        control.HorizontalAlignment = HAlignment.Center;
        control.VerticalAlignment = VAlignment.Top;
        control.Visible = !_pointerMode;

        _banner = control;
        AddChrome(control);
    }

    /// <summary>
    /// Shows the given page and hides the rest. Pages are siblings rather than a navigation stack, so
    /// this deliberately does not touch the back-button path.
    /// </summary>
    public void SetPage(int index)
    {
        if (_pages.Count == 0)
            return;

        index = Math.Clamp(index, 0, _pages.Count - 1);

        if (index == _pageIndex)
            return;

        for (var i = 0; i < _pages.Count; i++)
        {
            _pages[i].Visible = i == index;
        }

        _pageIndex = index;
        UpdatePageIndicator();

        // The selection belongs to the page that just went away, but the page that replaced it has not
        // been arranged yet - ArrangeCore skips hidden controls, so its sectors still have zero radius
        // and would match nothing. Re-resolve after the next layout pass instead of right now.
        _pointerSelectionDirty = true;
    }

    /// <inheritdoc />
    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        var result = base.ArrangeOverride(finalSize);

        if (_pointerSelectionDirty)
        {
            _pointerSelectionDirty = false;

            // Sector geometry is fresh at this point, so both the clamp and the selection resolve
            // against the page that is actually on screen.
            SetPointer(_pointer);
        }

        return result;
    }

    private void UpdatePageIndicator()
    {
        if (_pageDots != null)
            _pageDots.Active = _pageIndex;
    }

    /// <inheritdoc />
    protected override void MouseWheel(GUIMouseWheelEventArgs args)
    {
        base.MouseWheel(args);

        if (_pages.Count <= 1 || args.Delta.Y == 0f)
            return;

        // Clamped rather than wrapping, so the ends of the slot list are a hard stop. SetPage clamps.
        SetPage(_pageIndex - Math.Sign(args.Delta.Y));
        args.Handle();
    }

    private void SetHubLabel(string? text)
    {
        if (_hubLabel != null)
            _hubLabel.Text = text ?? string.Empty;
    }

    /// <summary>
    /// Switches the menu to virtual pointer driving: the real cursor stops interacting with the menu, and
    /// an internal pointer moved by raw mouse motion decides what is selected instead. This is what lets
    /// the wheel sit at a fixed place on screen while remaining aimable from wherever the cursor happens
    /// to be, and it sidesteps DPI scaling, off-window cursor positions and multi-monitor entirely.
    /// </summary>
    /// <remarks>
    /// No explicit dead zone is needed: the sectors already reject anything inside the ring's inner
    /// radius, so the hub is inert by construction.
    /// </remarks>
    public void EnablePointerMode()
    {
        if (_pointerMode)
            return;

        _pointerMode = true;

        // The menu defaults to ignoring mouse input, which would stop bubbled motion events reaching it.
        MouseFilter = MouseFilterMode.Pass;

        // Making the outer area button the topmost child means it always wins hit testing, so it becomes
        // the hovered control for every cursor position and we see every motion event. Being last also
        // means it draws after the sectors, so it can render the pointer on top of them.
        _outerAreaButtonIndex = MenuOuterAreaButton.GetPositionInParent();
        MenuOuterAreaButton.CoverEverything = true;
        MenuOuterAreaButton.Disabled = true;
        MenuOuterAreaButton.MouseFilter = MouseFilterMode.Pass;
        MenuOuterAreaButton.SetPositionLast();

        // Nothing else may react to the real cursor while the pointer is in charge, otherwise sectors
        // would light up under the physical cursor instead of under the pointer.
        // Releasing the key already cancels, so the hub's close/back button is redundant here - and it
        // would sit right on top of the hub label.
        ContextualButton.Visible = false;
        ContextualButton.MouseFilter = MouseFilterMode.Ignore;

        if (_banner != null)
            _banner.Visible = false;
        foreach (var button in EnumerateAllButtons())
        {
            button.MouseFilter = MouseFilterMode.Ignore;
        }

        SetPointer(Vector2.Zero);
    }

    /// <summary>
    /// Hands the menu back to the real cursor, restoring ordinary click-to-select behaviour. Used when a
    /// quick tap latches the wheel open instead of performing a flick selection.
    /// </summary>
    public void DisablePointerMode()
    {
        if (!_pointerMode)
            return;

        _pointerMode = false;

        // Paged menus keep listening for the wheel even when click-driven.
        MouseFilter = _pages.Count > 1 ? MouseFilterMode.Pass : MouseFilterMode.Ignore;

        MenuOuterAreaButton.CoverEverything = false;
        MenuOuterAreaButton.Disabled = false;
        MenuOuterAreaButton.MouseFilter = MouseFilterMode.Stop;
        MenuOuterAreaButton.PointerPosition = null;
        // Back below the sector containers, so sectors win hit testing again.
        MenuOuterAreaButton.SetPositionInParent(_outerAreaButtonIndex);

        ContextualButton.Visible = true;
        ContextualButton.MouseFilter = MouseFilterMode.Stop;

        if (_banner != null)
            _banner.Visible = true;
        foreach (var button in EnumerateAllButtons())
        {
            button.MouseFilter = MouseFilterMode.Stop;
        }

        if (PointerSelection is RadialMenuButtonWithSector sector)
            sector.ForceHighlight = false;

        PointerSelection = null;
        _pointer = Vector2.Zero;
        SetHubLabel(null);
    }

    /// <summary>
    /// Commits whatever the pointer rests on. Nested layer buttons navigate into their layer and keep the
    /// menu open, action buttons fire and close it, and an empty selection just closes.
    /// </summary>
    /// <returns>True if the menu is still open afterwards.</returns>
    public bool CommitPointerSelection()
    {
        var selection = PointerSelection;
        if (selection == null)
        {
            Close();
            return false;
        }

        if (selection.TargetLayer != null || selection.TargetLayerControlName != null)
        {
            if (selection.TargetLayer != null)
                TryToMoveToNewLayer(selection.TargetLayer);
            else
                TryToMoveToNewLayer(selection.TargetLayerControlName!);

            // Re-centre so the highlight and selection drop, then hand the menu back to the cursor: the
            // key that was being held is up by now, so leaving it pointer-driven would strand the player
            // in a layer they cannot commit anything from.
            SetPointer(Vector2.Zero);
            DisablePointerMode();
            return true;
        }

        if (GetSelectedOption() is RadialMenuActionOptionBase action) // SS220-emote-wheel-rework
            action.OnPressed.Invoke();

        Close();
        return false;
    }

    /// <inheritdoc />
    protected override void MouseMove(GUIMouseMoveEventArgs args)
    {
        base.MouseMove(args);

        if (!_pointerMode)
            return;

        // Relative is already divided by UIScale by the UI manager, so it arrives in virtual pixels -
        // the same space the sector geometry is expressed in.
        SetPointer(_pointer + args.Relative);
    }

    private void SetPointer(Vector2 position)
    {
        var limit = GetPointerLimit();
        if (limit > 0f)
        {
            var lengthSquared = position.LengthSquared();
            if (lengthSquared > limit * limit)
                position *= limit / MathF.Sqrt(lengthSquared);
        }

        _pointer = position;
        MenuOuterAreaButton.PointerPosition = position;
        UpdatePointerSelection();
    }

    /// <summary>
    /// Distance the pointer is allowed to travel from the hub. Follows the active container rather than
    /// being a constant, because the ring radius depends on button count, and lands just inside the outer
    /// edge - sitting exactly on it counts as outside the sector and would deselect.
    /// </summary>
    private float GetPointerLimit()
    {
        if (GetCurrentActiveLayer() is not RadialContainer { CalculatedOuterRadius: > 0f } container)
            return 0f;

        return MathF.Max(container.CalculatedInnerRadius, container.CalculatedOuterRadius - 2f);
    }

    private void UpdatePointerSelection()
    {
        RadialMenuButton? selected = null;

        foreach (var button in EnumerateActiveLayerButtons())
        {
            if (button is RadialMenuButtonWithSector sector && sector.ContainsRadialOffset(_pointer))
            {
                selected = button;
                break;
            }
        }

        if (selected == PointerSelection)
            return;

        if (PointerSelection is RadialMenuButtonWithSector previous)
            previous.ForceHighlight = false;

        PointerSelection = selected;

        if (selected is RadialMenuButtonWithSector current)
            current.ForceHighlight = true;

        SetHubLabel(GetSelectedOption()?.Label);
    }

    private RadialMenuOptionBase? GetSelectedOption()
    {
        return PointerSelection != null && _buttonModels.TryGetValue(PointerSelection, out var option)
            ? option
            : null;
    }

    private IEnumerable<RadialMenuButton> EnumerateAllButtons()
    {
        foreach (var child in Children)
        {
            if (child is not RadialContainer container)
                continue;

            foreach (var button in container.Children)
            {
                if (button is RadialMenuButton radial)
                    yield return radial;
            }
        }
    }

    private IEnumerable<RadialMenuButton> EnumerateActiveLayerButtons()
    {
        if (GetCurrentActiveLayer() is not RadialContainer container)
            yield break;

        foreach (var child in container.Children)
        {
            if (child is RadialMenuButton radial)
                yield return radial;
        }
    }

    // SS220-emote-wheel-rework-end

    #endregion
}
