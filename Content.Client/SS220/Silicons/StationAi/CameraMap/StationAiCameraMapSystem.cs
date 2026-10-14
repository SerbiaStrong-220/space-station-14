// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.SS220.Silicons.StationAi.CameraMap.UI;
using Content.Shared.SS220.Silicons.StationAi.CameraMap;
using Robust.Client.Audio;
using Robust.Client.Graphics;
using Robust.Shared.Player;

namespace Content.Client.SS220.Silicons.StationAi.CameraMap;

public sealed class StationAiCameraMapSystem : SharedStationAiCameraMapSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IOverlayManager _overlays = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    private StationAiCameraSwitchOverlay _switchOverlay = default!;

    /// <summary>
    ///     Cameras the local player pinned. Kept here so they survive closing the window.
    /// </summary>
    public readonly HashSet<NetEntity> Favorites = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationAiCameraMapComponent, AfterAutoHandleStateEvent>(OnAfterState);

        // Draws nothing until played, so it can stay registered.
        _switchOverlay = new StationAiCameraSwitchOverlay();
        _overlays.AddOverlay(_switchOverlay);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _overlays.RemoveOverlay(_switchOverlay);
    }

    protected override void ViewMoved(Entity<StationAiCameraMapComponent> ent)
    {
        if (!Timing.IsFirstTimePredicted)
            return;

        _audio.PlayGlobal(ent.Comp.SwitchSound, Filter.Local(), false);
        _switchOverlay.Play();
    }

    private void OnAfterState(Entity<StationAiCameraMapComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        CooldownChanged(ent);
    }

    protected override void CooldownChanged(Entity<StationAiCameraMapComponent> ent)
    {
        if (_ui.TryGetOpenUi<StationAiCameraMapBoundUserInterface>(ent.Owner, StationAiCameraMapUiKey.Key, out var bui))
            bui.UpdateCooldown();
    }

    /// <summary>
    ///     Camera power and wiring are server-side, so the client assumes the jump succeeds.
    /// </summary>
    protected override bool TryMoveEye(Entity<StationAiCameraMapComponent> ai, NetEntity target)
    {
        return true;
    }

    /// <summary>
    ///     Crew positions and AI vision are server-side, so the client assumes the jump succeeds.
    /// </summary>
    protected override bool TryMoveEyeToCrew(Entity<StationAiCameraMapComponent> ai, NetEntity sensor)
    {
        return true;
    }

    public void ToggleFavorite(NetEntity camera)
    {
        if (!Favorites.Remove(camera))
            Favorites.Add(camera);
    }
}
