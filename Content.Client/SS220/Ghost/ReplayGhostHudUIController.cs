// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.Replay.Spectator;
using Content.Client.Replay.UI;
using Content.Shared.Ghost;
using Content.Shared.SS220.Ghost;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.Replays.Playback;
using Robust.Client.Replays.UI;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.Ghost;

/// <summary>
/// Controls the free replay observer's HUDs locally and preserves selections when the observer is replaced.
/// </summary>
[UsedImplicitly]
public sealed partial class ReplayGhostHudUIController : UIController, IOnStateChanged<ReplayGhostState>
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IReplayPlaybackManager _replay = default!;

    private readonly Dictionary<ProtoId<GhostHudPrototype>, bool> _selections = [];
    private Button? _button;
    private GhostHudWindow? _window;

    public override void Initialize()
    {
        base.Initialize();
        _player.LocalPlayerAttached += OnPlayerAttached;
        _player.LocalPlayerDetached += OnPlayerDetached;
        _replay.ReplayPlaybackStopped += OnPlaybackStopped;
    }

    public void OnStateEntered(ReplayGhostState state)
    {
        ApplyObserverHuds();

        var widget = UIManager.GetActiveUIWidgetOrNull<ReplayControlWidget>();
        var buttons = widget?.StopButton.Parent;
        if (buttons == null)
            return;

        _button = new Button { Text = Loc.GetString("ghost-hud-window-title") };
        _button.OnPressed += _ => ToggleWindow();
        buttons.AddChild(_button);
    }

    public void OnStateExited(ReplayGhostState state)
    {
        CloseWindow();
        _window = null;
        _button?.Orphan();
        _button = null;
    }

    private void OnPlayerAttached(EntityUid uid)
    {
        ApplyObserverHuds();
    }

    private void OnPlayerDetached(EntityUid uid)
    {
        CloseWindow();
    }

    private void OnPlaybackStopped()
    {
        CloseWindow();
        _selections.Clear();
    }

    private Entity<GhostHudSettingsComponent>? GetObserver()
    {
        var uid = _player.LocalEntity;
        if (uid == null)
            return null;

        if (!EntityManager.TryGetComponent<GhostHudSettingsComponent>(uid.Value, out var settings))
            return null;

        if (!EntityManager.IsClientSide(uid.Value))
            return null;

        if (!EntityManager.HasComponent<ReplaySpectatorComponent>(uid.Value))
            return null;

        if (!EntityManager.HasComponent<GhostComponent>(uid.Value))
            return null;

        return (uid.Value, settings);
    }

    private void ApplyObserverHuds()
    {
        var observer = GetObserver();
        if (observer == null)
            return;

        foreach (var setting in observer.Value.Comp.Huds)
        {
            if (_selections.TryGetValue(setting.Id, out var enabled))
                setting.Enabled = enabled;

            ApplyHud(observer.Value, setting);
        }
    }

    private void ApplyHud(Entity<GhostHudSettingsComponent> observer, GhostHudSetting setting)
    {
        var prototype = _prototype.Index(setting.Id);
        if (!setting.Enabled)
        {
            EntityManager.RemoveComponents(observer, prototype.Components);
            return;
        }

        EntityManager.AddComponents(observer, prototype.Components);
    }

    private void ToggleWindow()
    {
        var observer = GetObserver();
        if (observer == null)
            return;

        if (_window == null)
        {
            _window = UIManager.CreateWindow<GhostHudWindow>();
            _window.OnHudToggled += OnHudToggled;
        }

        if (_window.IsOpen)
        {
            _window.Close();
            return;
        }

        List<GhostHudUiEntry> huds = [];
        foreach (var setting in observer.Value.Comp.Huds)
        {
            var prototype = _prototype.Index(setting.Id);
            huds.Add(new GhostHudUiEntry(setting.Id, prototype.Name, setting.Enabled));
        }
        _window.SetState(new GhostHudBoundUserInterfaceState(huds));
        _window.OpenCentered();
    }

    private void OnHudToggled(ProtoId<GhostHudPrototype> hud, bool enabled)
    {
        var observer = GetObserver();
        if (observer == null)
            return;

        foreach (var setting in observer.Value.Comp.Huds)
        {
            if (setting.Id != hud)
                continue;

            setting.Enabled = enabled;
            _selections[hud] = enabled;
            ApplyHud(observer.Value, setting);
            return;
        }
    }

    private void CloseWindow()
    {
        _window?.Close();
    }
}
