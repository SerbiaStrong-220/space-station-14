using System.Linq;
using System.Numerics;
using Content.Shared.Input;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Input;

namespace Content.Client.UserInterface.Controls;

[Virtual]
public class RadialMenu : BaseWindow
{
    /// <summary>
    /// Contextual button used to traverse through previous layers of the radial menu
    /// </summary>
    public RadialMenuContextualCentralTextureButton ContextualButton { get; }

    /// <summary>
    /// Button that represents outer area of menu (closes menu on outside clicks).
    /// </summary>
    public RadialMenuOuterAreaButton MenuOuterAreaButton { get; }

    /// <summary>
    /// Set a style class to be applied to the contextual button when it is set to move the user back through previous layers of the radial menu
    /// </summary>
    public string? BackButtonStyleClass
    {
        get
        {
            return _backButtonStyleClass;
        }

        set
        {
            _backButtonStyleClass = value;

            if (_path.Count > 0 && ContextualButton != null && _backButtonStyleClass != null)
                ContextualButton.SetOnlyStyleClass(_backButtonStyleClass);
        }
    }

    /// <summary>
    /// Set a style class to be applied to the contextual button when it will close the radial menu
    /// </summary>
    public string? CloseButtonStyleClass
    {
        get
        {
            return _closeButtonStyleClass;
        }

        set
        {
            _closeButtonStyleClass = value;

            if (_path.Count == 0 && ContextualButton != null && _closeButtonStyleClass != null)
                ContextualButton.SetOnlyStyleClass(_closeButtonStyleClass);
        }
    }

    private readonly List<Control> _path = new();
    private string? _backButtonStyleClass;
    private string? _closeButtonStyleClass;

    // SS220-emote-wheel-rework-begin

    private readonly HashSet<Control> _chrome = new();

    /// <summary>
    /// Adds a child as menu chrome - decoration such as a hub label that must never be treated as a
    /// navigable layer, and which stays visible as layers change.
    /// </summary>
    protected void AddChrome(Control control)
    {
        _chrome.Add(control);
        AddChild(control);
    }

    /// <summary>
    /// True for children that are chrome rather than navigable layers.
    /// </summary>
    protected bool IsChrome(Control child)
        => child == ContextualButton || child == MenuOuterAreaButton || _chrome.Contains(child);

    // SS220-emote-wheel-rework-end

    /// <summary>
    /// A free floating menu which enables the quick display of one or more radial containers
    /// </summary>
    /// <remarks>
    /// Only one radial container is visible at a time (each container forming a separate 'layer' within
    /// the menu), along with a contextual button at the menu center, which will either return the user
    /// to the previous layer or close the menu if there are no previous layers left to traverse.
    /// To create a functional radial menu, simply parent one or more named radial containers to it,
    /// and populate the radial containers with RadialMenuButtons. Setting the TargetLayer field of these
    /// buttons to the name of a radial conatiner will display the container in question to the user
    /// whenever it is clicked in additon to any other actions assigned to the button
    /// </remarks>
    public RadialMenu()
    {
        // Hide all starting children (if any) except the first (this is the active layer)
        if (ChildCount > 1)
        {
            for (int i = 1; i < ChildCount; i++)
                GetChild(i).Visible = false;
        }

        // Auto generate a contextual button for moving back through visited layers
        ContextualButton = new RadialMenuContextualCentralTextureButton
        {
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            SetSize = new Vector2(64f, 64f),
        };
        MenuOuterAreaButton = new RadialMenuOuterAreaButton();

        ContextualButton.OnButtonUp += _ => ReturnToPreviousLayer();
        MenuOuterAreaButton.OnButtonUp += _ => Close();
        AddChild(ContextualButton);
        AddChild(MenuOuterAreaButton);

        // Hide any further add children, unless its promoted to the active layer
        OnChildAdded += child =>
        {
            if (IsChrome(child)) // SS220-emote-wheel-rework: chrome is not a layer and stays visible
                return;

            child.Visible = GetCurrentActiveLayer() == child;
            SetupContextualButtonData(child);
        };
    }

    private void SetupContextualButtonData(Control child)
    {
        if (child is RadialContainer { Visible: true } container)
        {
            // SS220-emote-wheel-rework-begin
            // The container centers its sectors on its own arranged size, so deriving the hub and
            // outer-area hit areas from MinSize silently desyncs them from the sectors as soon as the
            // menu is not exactly MinSize big. Before the first arrange pass the container has no size
            // yet, so fall back to MinSize to keep the early OnChildAdded call usable.
            var containerSize = container.Size.LengthSquared() > 0 ? container.Size : MinSize;
            var parentCenter = container.Position + containerSize * 0.5f;
            ContextualButton.ParentCenter = parentCenter;
            MenuOuterAreaButton.ParentCenter = parentCenter;
            ContextualButton.InnerRadius = container.CalculatedInnerRadius;
            MenuOuterAreaButton.OuterRadius = container.CalculatedOuterRadius;
            // SS220-emote-wheel-rework-end
        }
    }

    /// <inheritdoc />
    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        var result = base.ArrangeOverride(finalSize);

        var currentLayer = GetCurrentActiveLayer();
        if (currentLayer != null)
        {
            SetupContextualButtonData(currentLayer);
        }

        return result;
    }

    protected Control? GetCurrentActiveLayer() // SS220-emote-wheel-rework: was private
    {
        var children = Children.Where(x => !IsChrome(x)); // SS220-emote-wheel-rework

        if (!children.Any())
            return null;

        return children.FirstOrDefault(x => x.Visible); // SS220-emote-wheel-rework: First throws when all layers are hidden
    }

    public bool TryToMoveToNewLayer(Control newLayer)
    {
        var currentLayer = GetCurrentActiveLayer();

        if (currentLayer == null)
            return false;

        var result = false;

        foreach (var child in Children)
        {
            if (IsChrome(child)) // SS220-emote-wheel-rework
                continue;

            // Hide layers which are not of interest
            if (result == true || child != newLayer)
            {
                child.Visible = false;
            }

            // Show the layer of interest
            else
            {
                child.Visible = true;
                SetupContextualButtonData(child);
                result = true;
            }
        }

        // Update the traversal path
        if (result)
            _path.Add(currentLayer);

        // Set the style class of the button
        if (_path.Count > 0 && ContextualButton != null && BackButtonStyleClass != null)
            ContextualButton.SetOnlyStyleClass(BackButtonStyleClass);

        return result;
    }

    public bool TryToMoveToNewLayer(string targetLayerControlName)
    {
        foreach (var child in Children)
        {
            if (child.Name == targetLayerControlName && child is RadialContainer)
            {
                return TryToMoveToNewLayer(child);
            }
        }

        return false;
    }

    public void ReturnToPreviousLayer()
    {
        // Close the menu if the traversal path is empty
        if (_path.Count == 0)
        {
            Close();
            return;
        }

        var lastChild = _path[^1];

        // Hide all children except the contextual button
        foreach (var child in Children)
        {
            if (!IsChrome(child)) // SS220-emote-wheel-rework
                child.Visible = false;
        }

        // Make the last visited layer visible, update the path list
        lastChild.Visible = true;
        _path.RemoveAt(_path.Count - 1);

        // Set the style class of the button
        if (_path.Count == 0 && ContextualButton != null && CloseButtonStyleClass != null)
            ContextualButton.SetOnlyStyleClass(CloseButtonStyleClass);
    }
}

/// <summary>
/// Base class for radial menu buttons. Excludes all actions except clicks and alt-clicks
/// from interactions.
/// </summary>
public abstract class RadialMenuButtonBase : BaseButton
{
    /// <inheritdoc />
    protected RadialMenuButtonBase()
    {
        EnableAllKeybinds = true;
    }

    /// <inheritdoc />
    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        if (args.Function == EngineKeyFunctions.UIClick
            || args.Function == ContentKeyFunctions.AltActivateItemInWorld)
        {
            base.KeyBindUp(args);
        }
    }
}

/// <summary>
/// Special button for closing radial menu or going back between radial menu levels.
/// Is looking like just <see cref="TextureButton "/> but considers whole space around
/// itself (til radial menu buttons) as itself in case of clicking. But this 'effect'
/// works only if control have parent, and ActiveContainer property is set.
/// Also considers all space outside of radial menu buttons as itself for clicking.
/// </summary>
public sealed class RadialMenuContextualCentralTextureButton : TextureButton
{
    /// <inheritdoc />
    public RadialMenuContextualCentralTextureButton()
    {
        EnableAllKeybinds = true;
    }

    public float InnerRadius { get; set; }

    public Vector2? ParentCenter { get; set; }

    /// <inheritdoc />
    protected override bool HasPoint(Vector2 point)
    {
        if (ParentCenter == null)
        {
            return base.HasPoint(point);
        }

        var distSquared = (point + Position - ParentCenter.Value).LengthSquared();

        var innerRadiusSquared = InnerRadius * InnerRadius;

        // comparing to squared values is faster, then making sqrt
        return distSquared < innerRadiusSquared;
    }

    /// <inheritdoc />
    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        if (args.Function == EngineKeyFunctions.UIClick
            || args.Function == ContentKeyFunctions.AltActivateItemInWorld)
        {
            base.KeyBindUp(args);
        }
    }
}

/// <summary>
/// Menu button for outer area of radial menu (covers everything 'outside').
/// </summary>
public sealed class RadialMenuOuterAreaButton : RadialMenuButtonBase
{
    public float OuterRadius { get; set; }

    public Vector2? ParentCenter { get; set; }

    // SS220-emote-wheel-rework-begin

    /// <summary>
    /// When true this button claims every point on screen rather than only the area outside the ring.
    /// Virtual pointer mode moves it to the top of the menu's children and turns this on, which makes it
    /// the single receiver of mouse motion for the whole menu and lets it draw the pointer above the
    /// sectors.
    /// </summary>
    public bool CoverEverything { get; set; }

    /// <summary>
    /// Virtual pointer offset from <see cref="ParentCenter"/> in virtual pixels, or null to draw nothing.
    /// </summary>
    public Vector2? PointerPosition { get; set; }

    /// <summary> Radius of the drawn virtual pointer, in virtual pixels. </summary>
    public float PointerRadius { get; set; } = 6f;

    /// <summary> Fill colour of the drawn virtual pointer. </summary>
    public Color PointerColor { get; set; } = Color.White;

    /// <summary> Outline colour of the drawn virtual pointer. </summary>
    public Color PointerOutlineColor { get; set; } = Color.Black;

    /// <inheritdoc />
    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (PointerPosition is not { } pointer || ParentCenter is not { } center)
            return;

        // Same convention as the sector buttons: local coordinates scaled into physical pixels.
        var origin = (center - Position + pointer) * UIScale;
        handle.DrawCircle(origin, PointerRadius * UIScale, PointerColor);
        handle.DrawCircle(origin, PointerRadius * UIScale, PointerOutlineColor, false);
    }

    // SS220-emote-wheel-rework-end

    /// <inheritdoc />
    protected override bool HasPoint(Vector2 point)
    {
        if (CoverEverything) // SS220-emote-wheel-rework
            return true; // SS220-emote-wheel-rework

        if (ParentCenter == null)
        {
            return base.HasPoint(point);
        }

        var distSquared = (point + Position - ParentCenter.Value).LengthSquared();

        var outerRadiusSquared = OuterRadius * OuterRadius;

        // comparing to squared values is faster, then making sqrt
        return distSquared > outerRadiusSquared;
    }
}

[Virtual]
public class RadialMenuButton : RadialMenuButtonBase
{
    /// <summary>
    /// Upon clicking this button the radial menu will be moved to the layer of this control.
    /// </summary>
    public Control? TargetLayer { get; set; }

    /// <summary>
    /// Other way to set navigation to other container, as <see cref="TargetLayer"/>,
    /// but using <see cref="Control.Name"/> property of target <see cref="RadialContainer"/>.
    /// </summary>
    public string? TargetLayerControlName { get; set; }

    /// <summary>
    /// A simple texture button that can move the user to a different layer within a radial menu
    /// </summary>
    public RadialMenuButton()
    {
        OnButtonUp += OnClicked;
    }

    private void OnClicked(ButtonEventArgs args)
    {
        if (TargetLayer == null && TargetLayerControlName == null)
            return;

        var parent = FindParentMultiLayerContainer(this);

        if (parent == null)
            return;

        if (TargetLayer != null)
        {
            parent.TryToMoveToNewLayer(TargetLayer);
        }
        else
        {
            parent.TryToMoveToNewLayer(TargetLayerControlName!);
        }
    }

    private RadialMenu? FindParentMultiLayerContainer(Control control)
    {
        foreach (var ancestor in control.GetSelfAndLogicalAncestors())
        {
            if (ancestor is RadialMenu menu)
                return menu;
        }

        return null;
    }
}

public interface IRadialMenuItemWithSector
{
    /// <summary>
    /// Angle in radian where button sector should start.
    /// </summary>
    public float AngleSectorFrom { set; }

    /// <summary>
    /// Angle in radian where button sector should end.
    /// </summary>
    public float AngleSectorTo { set; }

    /// <summary>
    /// Outer radius for drawing segment and pointer detection.
    /// </summary>
    public float OuterRadius { set; }

    /// <summary>
    /// Outer radius for drawing segment and pointer detection.
    /// </summary>
    public float InnerRadius { set; }

    /// <summary>
    /// Offset in radian by which menu button should be rotated.
    /// </summary>
    public float AngleOffset { set; }

    /// <summary>
    /// Coordinates of center in parent component - button container.
    /// </summary>
    public Vector2 ParentCenter { set; }
}

[Virtual]
public class RadialMenuButtonWithSector : RadialMenuButton, IRadialMenuItemWithSector
{
    private Vector2[]? _sectorPointsForDrawing;

    private float _angleSectorFrom;
    private float _angleSectorTo;
    private float _outerRadius;
    private float _innerRadius;
    private float _angleOffset;

    private bool _isWholeCircle;
    private Vector2? _parentCenter;

    private Color _backgroundColorSrgb = Color.ToSrgb(new Color(70, 73, 102, 128));
    private Color _hoverBackgroundColorSrgb = Color.ToSrgb(new Color(87, 91, 127, 128));
    private Color _borderColorSrgb = Color.ToSrgb(new Color(173, 216, 230, 70));
    private Color _hoverBorderColorSrgb = Color.ToSrgb(new Color(87, 91, 127, 128));

    /// <summary>
    /// Marker, that controls if border of segment should be rendered. Is false by default.
    /// </summary>
    /// <remarks>
    /// By default color of border is same as color of background. Use <see cref="BorderColor"/>
    /// and <see cref="HoverBorderColor"/> to change it.
    /// </remarks>
    public bool DrawBorder { get; set; } = false;

    /// <summary>
    /// Marker, that control should render background of all sector. Is true by default.
    /// </summary>
    public bool DrawBackground { get; set; } = true;

    /// <summary>
    /// Color of background in non-hovered state. Accepts RGB color, works with sRGB for DrawPrimitive internally.
    /// </summary>
    public Color BackgroundColor
    {
        get => Color.FromSrgb(_backgroundColorSrgb);
        set => _backgroundColorSrgb = Color.ToSrgb(value);
    }

    /// <summary>
    /// Color of background in hovered state. Accepts RGB color, works with sRGB for DrawPrimitive internally.
    /// </summary>
    public Color HoverBackgroundColor
    {
        get => Color.FromSrgb(_hoverBackgroundColorSrgb);
        set => _hoverBackgroundColorSrgb = Color.ToSrgb(value);
    }

    /// <summary>
    /// Color of button border. Accepts RGB color, works with sRGB for DrawPrimitive internally.
    /// </summary>
    public Color BorderColor
    {
        get => Color.FromSrgb(_borderColorSrgb);
        set => _borderColorSrgb = Color.ToSrgb(value);
    }

    /// <summary>
    /// Color of button border when button is hovered. Accepts RGB color, works with sRGB for DrawPrimitive internally.
    /// </summary>
    public Color HoverBorderColor
    {
        get => Color.FromSrgb(_hoverBorderColorSrgb);
        set => _hoverBorderColorSrgb = Color.ToSrgb(value);
    }

    /// <summary>
    /// Color of separator lines.
    /// Separator lines are used to visually separate sector of radial menu items.
    /// </summary>
    public Color SeparatorColor { get; set; } = new Color(128, 128, 128, 128);

    // SS220-emote-wheel-rework-begin

    /// <summary>
    /// Renders the sector in its hovered state regardless of where the real cursor is. Virtual pointer
    /// mode drives selection by pointer angle rather than by real mouse hover, so the engine's own hover
    /// tracking cannot be used for the highlight.
    /// </summary>
    public bool ForceHighlight { get; set; }

    /// <summary>
    /// How much further out, in virtual pixels, the sector is drawn while highlighted. Purely visual -
    /// hit testing keeps using the unexpanded radius, so the bump cannot change what is selected.
    /// </summary>
    public float HighlightExpansion { get; set; }

    /// <summary>
    /// Tests whether an offset from the wheel centre falls inside this button's sector. Split out of
    /// <see cref="HasPoint"/> so virtual pointer selection reuses the exact same geometry rather than
    /// reimplementing it.
    /// </summary>
    public bool ContainsRadialOffset(Vector2 offsetFromCenter)
    {
        var distSquared = offsetFromCenter.LengthSquared();
        if (distSquared >= _outerRadius * _outerRadius || distSquared <= _innerRadius * _innerRadius)
            return false;

        // Flip Y to get from ui coordinates to natural coordinates
        var angle = MathF.Atan2(-offsetFromCenter.Y, offsetFromCenter.X) - _angleOffset;
        if (angle < 0)
        {
            // atan2 range is -pi->pi, while angle sectors are
            // 0->2pi, so remap the result into that range
            angle = MathF.PI * 2 + angle;
        }

        return angle >= _angleSectorFrom && angle < _angleSectorTo;
    }

    // SS220-emote-wheel-rework-end

    /// <inheritdoc />
    float IRadialMenuItemWithSector.AngleSectorFrom
    {
        set
        {
            _angleSectorFrom = value;
            _isWholeCircle = IsWholeCircle(value, _angleSectorTo);
        }
    }

    /// <inheritdoc />
    float IRadialMenuItemWithSector.AngleSectorTo
    {
        set
        {
            _angleSectorTo = value;
            _isWholeCircle = IsWholeCircle(_angleSectorFrom, value);
        }
    }

    /// <inheritdoc />
    float IRadialMenuItemWithSector.OuterRadius { set => _outerRadius = value; }

    /// <inheritdoc />
    float IRadialMenuItemWithSector.InnerRadius { set => _innerRadius = value; }

    /// <inheritdoc />
    public float AngleOffset { set => _angleOffset = value; }

    /// <inheritdoc />
    Vector2 IRadialMenuItemWithSector.ParentCenter { set => _parentCenter = value; }

    /// <summary>
    /// A simple texture button that can move the user to a different layer within a radial menu
    /// </summary>
    public RadialMenuButtonWithSector()
    {
    }

    /// <inheritdoc />
    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (_parentCenter == null)
        {
            return;
        }

        // draw sector where space that button occupies actually is
        var containerCenter = (_parentCenter.Value - Position) * UIScale;

        var angleFrom = _angleSectorFrom + _angleOffset;
        var angleTo = _angleSectorTo + _angleOffset;
        // SS220-emote-wheel-rework-begin
        var highlighted = DrawMode == DrawModeEnum.Hover || ForceHighlight;
        var innerRadius = _innerRadius * UIScale;
        var outerRadius = (highlighted ? _outerRadius + HighlightExpansion : _outerRadius) * UIScale;
        // SS220-emote-wheel-rework-end
        if (DrawBackground)
        {
            var segmentColor = highlighted // SS220-emote-wheel-rework
                ? _hoverBackgroundColorSrgb
                : _backgroundColorSrgb;

            DrawAnnulusSector(handle, containerCenter, innerRadius, outerRadius, angleFrom, angleTo, segmentColor);
        }

        if (DrawBorder)
        {
            var borderColor = highlighted // SS220-emote-wheel-rework
                ? _hoverBorderColorSrgb
                : _borderColorSrgb;
            DrawAnnulusSector(handle, containerCenter, innerRadius, outerRadius, angleFrom, angleTo, borderColor, false);
        }

        if (!_isWholeCircle && DrawBorder)
        {
            DrawSeparatorLines(handle, containerCenter, innerRadius, outerRadius, angleFrom, angleTo, SeparatorColor);
        }
    }

    /// <inheritdoc />
    protected override bool HasPoint(Vector2 point)
    {
        if (_parentCenter == null)
        {
            return base.HasPoint(point);
        }

        return ContainsRadialOffset(point + Position - _parentCenter.Value); // SS220-emote-wheel-rework
    }

    /// <summary>
    /// Draw segment between two concentrated circles from and to certain angles.
    /// </summary>
    /// <param name="drawingHandleScreen">Drawing handle, to which rendering should be delegated.</param>
    /// <param name="center">Point where circle center should be.</param>
    /// <param name="radiusInner">Radius of internal circle.</param>
    /// <param name="radiusOuter">Radius of external circle.</param>
    /// <param name="angleSectorFrom">Angle in radian, from which sector should start.</param>
    /// <param name="angleSectorTo">Angle in radian, from which sector should start.</param>
    /// <param name="color">Color for drawing.</param>
    /// <param name="filled">Should figure be filled, or have only border.</param>
    private void DrawAnnulusSector(
        DrawingHandleScreen drawingHandleScreen,
        Vector2 center,
        float radiusInner,
        float radiusOuter,
        float angleSectorFrom,
        float angleSectorTo,
        Color color,
        bool filled = true
    )
    {
        const float minimalSegmentSize = MathF.Tau / 128f;

        var requestedSegmentSize = angleSectorTo - angleSectorFrom;
        var segmentCount = (int)(requestedSegmentSize / minimalSegmentSize) + 1;
        var anglePerSegment = requestedSegmentSize / (segmentCount - 1);

        var bufferSize = segmentCount * 2;
        if (_sectorPointsForDrawing == null || _sectorPointsForDrawing.Length != bufferSize)
        {
            _sectorPointsForDrawing ??= new Vector2[bufferSize];
        }

        for (var i = 0; i < segmentCount; i++)
        {
            var angle = angleSectorFrom + anglePerSegment * i;

            // Flip Y to get from ui coordinates to natural coordinates
            var unitPos = new Vector2(MathF.Cos(angle), -MathF.Sin(angle));
            var outerPoint = center + unitPos * radiusOuter;
            var innerPoint = center + unitPos * radiusInner;
            if (filled)
            {
                // to make filled sector we need to create strip from triangles
                _sectorPointsForDrawing[i * 2] = outerPoint;
                _sectorPointsForDrawing[i * 2 + 1] = innerPoint;
            }
            else
            {
                // to make border of sector we need points ordered as sequences on radius
                _sectorPointsForDrawing[i] = outerPoint;
                _sectorPointsForDrawing[bufferSize - 1 - i] = innerPoint;
            }
        }

        var type = filled
            ? DrawPrimitiveTopology.TriangleStrip
            : DrawPrimitiveTopology.LineStrip;
        drawingHandleScreen.DrawPrimitives(type, _sectorPointsForDrawing, color);
    }

    private static void DrawSeparatorLines(
        DrawingHandleScreen drawingHandleScreen,
        Vector2 center,
        float radiusInner,
        float radiusOuter,
        float angleSectorFrom,
        float angleSectorTo,
        Color color
    )
    {
        var fromPoint = new Angle(-angleSectorFrom).RotateVec(Vector2.UnitX);
        drawingHandleScreen.DrawLine(
            center + fromPoint * radiusOuter,
            center + fromPoint * radiusInner,
            color
        );

        var toPoint = new Angle(-angleSectorTo).RotateVec(Vector2.UnitX);
        drawingHandleScreen.DrawLine(
            center + toPoint * radiusOuter,
            center + toPoint * radiusInner,
            color
        );
    }

    private static bool IsWholeCircle(float angleSectorFrom, float angleSectorTo)
    {
        return new Angle(angleSectorFrom).EqualsApprox(new Angle(angleSectorTo));
    }
}
