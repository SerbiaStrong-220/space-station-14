using System.Numerics;
using Content.Client.UserInterface.Controls;
using Content.Shared.Chat.Prototypes;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.EmoteWheel;

/// <summary>
/// The wheel as it will appear in game, and the surface the player edits on: clicking a sector picks the
/// place the next emote goes. Built on the same <see cref="RadialContainer"/> and sector buttons the real
/// wheel uses, so this is the arrangement they will get rather than an approximation of it.
/// </summary>
public sealed class EmoteWheelPreview : Control
{
    // Close to the real wheel's size rather than a shrunken copy: names have to be readable here too,
    // and the settings window has room for it.
    private const float Radius = 190f;
    private const float InnerRadius = 90f;
    private const float OuterRadius = 262f;

    /// <summary> Label sizing, matched to the sector chord at eight buttons on this radius. </summary>
    private const float LabelWidth = 138f;

    private const int LabelWrapChars = 14;

    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IEntityManager _entities = default!;

    private readonly RadialContainer _container;

    /// <summary> Raised with the sector index when the player clicks one. </summary>
    public event Action<int>? OnCellPressed;

    /// <summary> Raised when the player clicks anywhere that is not a sector, i.e. to deselect. </summary>
    public event Action? OnBackgroundPressed;

    /// <summary> Raised with the sector index when the player right-clicks one, i.e. to empty it. </summary>
    public event Action<int>? OnCellRightPressed;

    /// <summary> Sector currently being edited, drawn highlighted, or null when nothing is selected. </summary>
    public int? SelectedCell { get; set; }

    public EmoteWheelPreview()
    {
        IoCManager.InjectDependencies(this);

        MinSize = new Vector2(OuterRadius * 2f, OuterRadius * 2f);

        _container = new RadialContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            FixedRadius = Radius,
            FixedInnerRadius = InnerRadius,
            FixedOuterRadius = OuterRadius,
            CenterFirstItemAtTop = true,
            ReserveSpaceForHiddenChildren = false,
        };

        // Clicks that reach this control rather than a sector are clicks on empty space, which deselect.
        MouseFilter = MouseFilterMode.Stop;

        AddChild(_container);
    }

    /// <inheritdoc />
    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Function != EngineKeyFunctions.UIClick)
            return;

        OnBackgroundPressed?.Invoke();
        args.Handle();
    }

    /// <summary> Redraws the preview for the given slot contents. </summary>
    public void Update(IReadOnlyList<ProtoId<EmotePrototype>?> slot)
    {
        _container.RemoveAllChildren();

        var sprites = _entities.System<SpriteSystem>();

        for (var index = 0; index < slot.Count; index++)
        {
            var cell = slot[index];

            // Empty cells still take a sector: they are where the player clicks to fill them, and showing
            // them keeps the preview honest about the gaps the wheel will have.
            var button = new RadialMenuButtonWithSector
            {
                DrawBorder = true,
                DrawBackground = true,
                ForceHighlight = index == SelectedCell,
            };

            if (cell.HasValue && _prototypes.TryIndex(cell.Value, out var emote))
            {
                var box = new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Vertical,
                    HorizontalAlignment = HAlignment.Center,
                    VerticalAlignment = VAlignment.Center,
                    SeparationOverride = 2,
                };

                box.AddChild(new TextureRect
                {
                    Texture = sprites.Frame0(emote.Icon),
                    HorizontalAlignment = HAlignment.Center,
                    Stretch = TextureRect.StretchMode.KeepCentered,
                });

                box.AddChild(new Label
                {
                    // Same wrapping the real wheel uses, so long names break across lines here too
                    // rather than being cut off.
                    Text = SimpleRadialMenu.BreakLongLabel(Loc.GetString(emote.Name), LabelWrapChars),
                    Align = Label.AlignMode.Center,
                    HorizontalAlignment = HAlignment.Center,
                    // SetWidth rather than MaxWidth: a clipped Label measures as zero width and would be
                    // laid out as though it were not there.
                    SetWidth = LabelWidth,
                    ClipText = true,
                });

                button.AddChild(box);
            }
            else
            {
                button.SetSize = new Vector2(48f, 48f);
            }

            var cellIndex = index;
            button.OnPressed += _ => OnCellPressed?.Invoke(cellIndex);

            // BaseButton only turns UIClick into OnPressed, so right-click is taken off the raw keybind.
            button.OnKeyBindDown += args =>
            {
                if (args.Function != EngineKeyFunctions.UIRightClick)
                    return;

                OnCellRightPressed?.Invoke(cellIndex);
                args.Handle();
            };

            _container.AddChild(button);
        }
    }
}
