// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.Administration.Managers;
using Content.Client.SS220.Overlays;
using Content.Shared.Administration;
using Content.Shared.SS220.Zone.Components;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.SS220.Zone.UI;

public sealed class ZonesControlUIController : UIController
{
    [Dependency] private readonly IClientAdminManager _clientAdmin = default!;

    private ZonesControlWindow? _window;
    private ZoneBoxesOverlayProvider _overlayProvider = default!;

    public EntityUid? EditingZone => _window?.EditingZoneEntry?.ZoneEntity.Owner;

    public override void Initialize()
    {
        base.Initialize();

        var overlay = BoxesOverlay.GetOverlay();
        _overlayProvider = overlay.EnsureProvider<ZoneBoxesOverlayProvider>();

        _clientAdmin.AdminStatusUpdated += OnAdminStatusUpdated;
    }

    private void OnAdminStatusUpdated()
    {
        ResetOverlay();
    }

    public void SetOverlay(bool value)
    {
        if (!_clientAdmin.HasFlag(AdminFlags.Mapping))
            value = false;

        _overlayProvider.Active = value;
    }

    public void ToggleWindow()
    {
        EnsureWindow();
        if (_window == null)
            return;

        if (!_window.IsOpen)
            _window.Open();
        else
            _window.Close();
    }

    public void RefreshWindow()
    {
        _window?.Refresh();
    }

    private void EnsureWindow()
    {
        if (_window is { Disposed: false })
            return;

        _window = UIManager.CreateWindow<ZonesControlWindow>();
        LayoutContainer.SetAnchorAndMarginPreset(_window, LayoutContainer.LayoutPreset.TopRight);
    }

    private void ResetOverlay()
    {
        SetOverlay(_overlayProvider.Active);
    }

    public sealed partial class ZoneBoxesOverlayProvider : BoxesOverlay.BoxesOverlayProvider
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly IUserInterfaceManager _ui = default!;

        private readonly ZonesControlUIController _controller = default!;

        private const float ZoneColorAlpha = 0.125f;
        private const float EditingZoneColorAlpha = 0.25f;

        public ZoneBoxesOverlayProvider() : base()
        {
            _controller = _ui.GetUIController<ZonesControlUIController>();
        }

        public override List<BoxesOverlay.BoxOverlayData> GetBoxesDatas()
        {
            List<BoxesOverlay.BoxOverlayData> overlayData = [];

            var editingZone = _controller.EditingZone;
            var query = _entityManager.AllEntityQueryEnumerator<ZoneComponent>();
            while (query.MoveNext(out var uid, out var zoneComp))
            {
                if (_entityManager.Deleted(uid))
                    continue;

                var alpha = uid == editingZone ? EditingZoneColorAlpha : ZoneColorAlpha;
                var color = zoneComp.Color.WithAlpha(alpha);
                foreach (var box in zoneComp.Area)
                    overlayData.Add(new BoxesOverlay.BoxOverlayData(uid, box, color));
            }

            return overlayData;
        }
    }
}
