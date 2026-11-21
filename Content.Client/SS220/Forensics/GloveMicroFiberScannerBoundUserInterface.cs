// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.Stylesheets.Palette;
using Content.Client.UserInterface.Controls;
using Content.Shared.SS220.Forensics;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.Forensics;

/// <summary>
/// Selects the portable scanner's collection mode through the standard radial menu.
/// </summary>
public sealed class GloveMicroFiberScannerBoundUserInterface : BoundUserInterface
{
    private static readonly EntProtoId SurfaceModeIcon = "GloveMicroFiberSwab";
    private static readonly EntProtoId OwnModeIcon = "ClothingHandsGlovesColorBlack";
    private static readonly Color SelectedOptionColor = Palettes.Green.Element.WithAlpha(128);
    private static readonly Color SelectedOptionHoverColor = Palettes.Green.HoveredElement.WithAlpha(128);

    private SimpleRadialMenu? _menu;

    public GloveMicroFiberScannerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        if (!EntMan.TryGetComponent<GloveMicroFiberScannerComponent>(Owner, out var scanner))
            return;

        _menu = this.CreateWindow<SimpleRadialMenu>();
        RadialMenuOptionBase[] options =
        [
            CreateOption(GloveMicroFiberScannerMode.SurfaceTraces, SurfaceModeIcon,
                "glove-micro-fiber-scanner-mode-surface", scanner.Mode),
            CreateOption(GloveMicroFiberScannerMode.OwnSample, OwnModeIcon,
                "glove-micro-fiber-scanner-mode-own", scanner.Mode),
        ];
        _menu.SetButtons(options);
        _menu.OpenOverMouseScreenPosition();
    }

    private RadialMenuOptionBase CreateOption(GloveMicroFiberScannerMode mode, EntProtoId icon, LocId tooltip,
        GloveMicroFiberScannerMode selectedMode)
    {
        var selected = mode == selectedMode;
        return new RadialMenuActionOption<GloveMicroFiberScannerMode>(SelectMode, mode)
        {
            IconSpecifier = RadialMenuIconSpecifier.With(icon),
            ToolTip = Loc.GetString(tooltip),
            BackgroundColor = selected ? SelectedOptionColor : null,
            HoverBackgroundColor = selected ? SelectedOptionHoverColor : null,
        };
    }

    private void SelectMode(GloveMicroFiberScannerMode mode)
    {
        EntMan.RaisePredictiveEvent(new GloveMicroFiberScannerSetModeEvent(EntMan.GetNetEntity(Owner), mode));
    }
}
