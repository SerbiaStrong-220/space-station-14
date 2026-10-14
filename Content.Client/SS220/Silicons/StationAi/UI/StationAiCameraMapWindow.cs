// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Numerics;
using System.Linq;
using Content.Client.Pinpointer.UI;
using Content.Client.UserInterface.Controls;
using Content.Shared.SS220.Silicons.StationAi;
using Content.Shared.StatusIcon;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Shared.Timing;

namespace Content.Client.SS220.Silicons.StationAi.UI;

public sealed class StationAiCameraMapWindow : FancyWindow
{
    private static readonly Color ActiveColor = new(125, 195, 170);
    // Bar fills are dimmed so the white countdown text stays readable over both halves.
    private static readonly Color ReadyFillColor = new(56, 110, 92);
    private static readonly Color CooldownFillColor = new(140, 100, 36);
    private static readonly Color MutedColor = new(140, 146, 160);
    private static readonly Color FailureFillColor = new(130, 44, 44);
    private static readonly Color DeadColor = new(214, 82, 82);
    private static readonly TimeSpan FailureDuration = TimeSpan.FromSeconds(2.5);
    private readonly StationAiCameraMapControl _map;
    private readonly BoxContainer _body;
    private readonly BoxContainer _sidebar;
    private readonly Texture _blip;
    private readonly LineEdit _search;
    private readonly BoxContainer _cameraList;
    private readonly ScrollContainer _scroll;
    private readonly OptionButton _status;
    private readonly ProgressBar _cooldownBar;
    private readonly StyleBoxFlat _cooldownFill;
    private readonly Label _cooldown;
    private readonly IGameTiming _timing = IoCManager.Resolve<IGameTiming>();
    private TimeSpan _nextTeleport;
    private HashSet<NetEntity> _favorites = new();
    private List<(NetEntity Entity, string Name, bool Active)> _cameras = new();
    // Rows are built once per camera state and reused by search/filter, which only re-parents them.
    private Dictionary<(NetEntity Entity, string Name, bool Active), CameraRow> _rows = new();
    private readonly List<Control> _transient = new();
    private int _cooldownTextKey = int.MinValue;
    private bool _hasState;
    private readonly Button _camerasTab;
    private readonly Button _crewTab;
    private bool _showCrew;
    private List<StationAiCameraMapCrew> _crew = new();
    // Positions change every refresh; the list is only rebuilt when something shown in a row changes.
    private List<(NetEntity, string, string, string, int, bool)> _crewKeys = new();
    private readonly HashSet<NetEntity> _crewBlips = new();
    // Inactive cameras and uncovered beacons stay on the map, dimmed, so gaps in coverage are visible.
    private readonly Dictionary<NetEntity, bool> _inactiveBlips = new();
    private EntityUid _grid;
    private List<StationAiCameraMapTarget> _targets = new();
    private List<StationAiCameraMapCrew> _crewState = new();
    private TimeSpan _failureUntil;
    private string? _failureText;
    public event Action<NetEntity>? OnTargetSelected;
    public event Action? OnJumpToCore;
    public event Action<NetEntity>? OnCrewSelected;
    /// <summary>Raised when the server starts a new cooldown, i.e. the view actually moved.</summary>
    public event Action? OnCameraSwitched;

    public StationAiCameraMapWindow()
    {
        Title = Loc.GetString("station-ai-camera-map-title");
        MinSize = new Vector2(560, 520);
        SetSize = new Vector2(1020, 740);
        _blip = IoCManager.Resolve<IEntityManager>().System<SpriteSystem>().Frame0(
            new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/NavMap/beveled_circle.png")));
        var layout = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical };
        _body = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            VerticalExpand = true,
            SeparationOverride = 12,
            // Wider than the FancyWindow drag margin so the map and list never cover the resize edges.
            Margin = new Thickness(10, 0, 10, 6),
        };
        _map = new StationAiCameraMapControl { HorizontalExpand = true, VerticalExpand = true };
        _map.TrackedEntitySelectedAction += target =>
        {
            if (target is not { } uid)
                return;
            if (_crewBlips.Contains(uid))
                SelectCrew(uid);
            else if (_inactiveBlips.TryGetValue(uid, out var isCamera))
                ShowFailure(isCamera ? StationAiCameraMapFailure.CameraInactive : StationAiCameraMapFailure.OutOfView);
            else
                SelectTarget(uid);
        };
        _body.AddChild(_map);
        _sidebar = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            VerticalExpand = true,
            SeparationOverride = 6,
        };
        // Teleport cooldown: the bar fills up while recharging, the label counts down on top of it.
        _cooldownFill = new StyleBoxFlat(ReadyFillColor);
        _cooldownBar = new ProgressBar
        {
            MinValue = 0,
            MaxValue = 1,
            Value = 1,
            HorizontalExpand = true,
            BackgroundStyleBoxOverride = new StyleBoxFlat(new Color(30, 32, 40)),
            ForegroundStyleBoxOverride = _cooldownFill,
        };
        _cooldown = new Label
        {
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            FontColorOverride = Color.White,
        };
        _sidebar.AddChild(new Control
        {
            MinHeight = 24,
            Children = { _cooldownBar, _cooldown },
        });
        var tabs = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, SeparationOverride = 2 };
        var tabGroup = new ButtonGroup();
        _camerasTab = new Button
        {
            Text = Loc.GetString("station-ai-camera-map-tab-cameras"),
            ToggleMode = true,
            Group = tabGroup,
            Pressed = true,
            HorizontalExpand = true,
            StyleClasses = { "OpenRight" },
        };
        _crewTab = new Button
        {
            Text = Loc.GetString("station-ai-camera-map-tab-crew"),
            ToggleMode = true,
            Group = tabGroup,
            HorizontalExpand = true,
            StyleClasses = { "OpenLeft" },
        };
        _camerasTab.OnPressed += _ => SetTab(false);
        _crewTab.OnPressed += _ => SetTab(true);
        tabs.AddChild(_camerasTab);
        tabs.AddChild(_crewTab);
        _sidebar.AddChild(tabs);
        var filters = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, SeparationOverride = 6 };
        _search = new LineEdit
        {
            PlaceHolder = Loc.GetString("station-ai-camera-map-search"),
            HorizontalExpand = true,
        };
        filters.AddChild(_search);
        _status = new OptionButton { MinWidth = 110 };
        _status.AddItem(Loc.GetString("station-ai-camera-map-status-all"), 0);
        _status.AddItem(Loc.GetString("station-ai-camera-map-status-active"), 1);
        _status.AddItem(Loc.GetString("station-ai-camera-map-status-inactive"), 2);
        _status.SelectId(0);
        filters.AddChild(_status);
        _sidebar.AddChild(filters);
        _cameraList = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, SeparationOverride = 2 };
        _scroll = new ScrollContainer { VerticalExpand = true, HScrollEnabled = false };
        _scroll.AddChild(_cameraList);
        _sidebar.AddChild(_scroll);
        _body.AddChild(_sidebar);
        layout.AddChild(_body);
        // Pinned under both the map and the list in every layout.
        var core = new Button
        {
            Text = Loc.GetString("station-ai-camera-map-return-core"),
            ToolTip = Loc.GetString("station-ai-camera-map-return-core-tooltip"),
            MinHeight = 30,
            Margin = new Thickness(10, 0, 10, 10),
        };
        core.OnPressed += _ => OnJumpToCore?.Invoke();
        layout.AddChild(core);
        ContentsContainer.AddChild(layout);
        _status.OnItemSelected += args =>
        {
            _status.SelectId(args.Id);
            RebuildCameraList();
            _scroll.SetScrollValue(Vector2.Zero);
        };
        _search.OnTextChanged += _ =>
        {
            RebuildCameraList();
            _scroll.SetScrollValue(Vector2.Zero);
        };
        RebuildCameraList();
        ApplyLayout(SetSize);
    }

    protected override void Resized()
    {
        base.Resized();
        ApplyLayout(Size);
    }

    private void ApplyLayout(Vector2 size)
    {
        var layout = StationAiCameraMapLayout.Calculate(size.X, size.Y);
        _body.Orientation = layout.Portrait
            ? BoxContainer.LayoutOrientation.Vertical
            : BoxContainer.LayoutOrientation.Horizontal;
        _map.SetHeight = layout.MapHeight;
        _map.VerticalExpand = !layout.Portrait;
        _sidebar.SetWidth = layout.SidebarWidth;
        _sidebar.HorizontalExpand = layout.Portrait;
    }

    private void SelectTarget(NetEntity target)
    {
        if (_timing.CurTime >= _nextTeleport)
            OnTargetSelected?.Invoke(target);
    }

    private void SelectCrew(NetEntity sensor)
    {
        if (_timing.CurTime >= _nextTeleport)
            OnCrewSelected?.Invoke(sensor);
    }

    private void SetTab(bool crew)
    {
        if (_showCrew == crew)
            return;

        _showCrew = crew;
        _status.Visible = !crew;
        _search.PlaceHolder = Loc.GetString(crew ? "station-ai-camera-map-search-crew" : "station-ai-camera-map-search");
        RefreshBlips();
        RebuildCameraList();
        _scroll.SetScrollValue(Vector2.Zero);
    }

    /// <summary>Briefly replaces the cooldown text with the reason the server refused a jump.</summary>
    public void ShowFailure(StationAiCameraMapFailure failure)
    {
        _failureText = Loc.GetString(failure switch
        {
            StationAiCameraMapFailure.NoCoordinates => "station-ai-camera-map-no-coordinates",
            StationAiCameraMapFailure.CameraInactive => "station-ai-camera-map-camera-inactive",
            _ => "station-ai-camera-map-out-of-view",
        });
        _failureUntil = _timing.CurTime + FailureDuration;
        _cooldownTextKey = int.MinValue;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        if (_failureText != null)
        {
            if (_timing.CurTime < _failureUntil)
            {
                if (_cooldownTextKey == -2)
                    return;
                _cooldownTextKey = -2;
                _cooldownBar.Value = 1;
                _cooldownFill.BackgroundColor = FailureFillColor;
                _cooldown.Text = _failureText;
                return;
            }

            _failureText = null;
            _cooldownTextKey = int.MinValue;
        }

        var remaining = _nextTeleport - _timing.CurTime;
        var ready = remaining <= TimeSpan.Zero;
        _cooldownBar.Value = ready
            ? 1
            : 1 - (float) Math.Clamp(remaining / StationAiCameraMapComponent.TeleportCooldown, 0, 1);
        // Text and colour change ten times a second at most; skip the relayout on other frames.
        var key = ready ? -1 : (int) Math.Ceiling(remaining.TotalSeconds * 10);
        if (key == _cooldownTextKey)
            return;

        _cooldownTextKey = key;
        _cooldownFill.BackgroundColor = ready ? ReadyFillColor : CooldownFillColor;
        _cooldown.Text = ready
            ? Loc.GetString("station-ai-camera-map-ready")
            : Loc.GetString("station-ai-camera-map-cooldown", ("seconds", (key / 10f).ToString("0.0")));
    }

    public void SetFavorites(HashSet<NetEntity> favorites)
    {
        _favorites = favorites;
        RebuildCameraList();
    }

    private void RebuildCameraList()
    {
        _cameraList.RemoveAllChildren();
        foreach (var control in _transient)
            control.Dispose();
        _transient.Clear();

        if (_showCrew)
        {
            RebuildCrewList();
            return;
        }

        var search = _search.Text.Trim();
        var matches = _cameras.Where(camera =>
            camera.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase) &&
            (_status.SelectedId == 0 || camera.Active == (_status.SelectedId == 1))).ToList();
        var anyFavorite = matches.Any(camera => _favorites.Contains(camera.Entity));
        foreach (var favorite in new[] { true, false })
        {
            var cameras = matches.Where(camera => _favorites.Contains(camera.Entity) == favorite).ToList();
            if (cameras.Count == 0)
                continue;

            var header = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                Margin = new Thickness(2, 6, 4, 2),
            };
            header.AddChild(new Label
            {
                Text = Loc.GetString(favorite ? "station-ai-camera-map-favorites" :
                    anyFavorite ? "station-ai-camera-map-other" : "station-ai-camera-map-all"),
                HorizontalExpand = true,
            });
            var active = cameras.Count(camera => camera.Active);
            header.AddChild(new Label
            {
                Text = Loc.GetString("station-ai-camera-map-active-count", ("count", active)),
                ToolTip = Loc.GetString("station-ai-camera-map-active-count-tooltip", ("active", active), ("total", cameras.Count)),
                FontColorOverride = active > 0 ? ActiveColor : MutedColor,
            });
            AddTransient(header);
            foreach (var camera in cameras)
            {
                var row = _rows[camera];
                row.SetFavorite(favorite);
                _cameraList.AddChild(row.Root);
            }
        }

        if (matches.Count == 0)
        {
            AddTransient(new Label
            {
                Text = Loc.GetString("station-ai-camera-map-empty"),
                FontColorOverride = MutedColor,
                HorizontalAlignment = HAlignment.Center,
                Margin = new Thickness(0, 12),
            });
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        // Rows hidden by the filter are not children, so the window would not dispose them itself.
        foreach (var row in _rows.Values)
            row.Root.Dispose();
        _rows.Clear();
    }

        private void RebuildCrewList()
    {
        var search = _search.Text.Trim();
        var matches = _crew.Where(crew =>
                crew.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                crew.Job.Contains(search, StringComparison.CurrentCultureIgnoreCase))
            .ToList();
        var header = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            Margin = new Thickness(2, 6, 4, 2),
        };
        header.AddChild(new Label
        {
            Text = Loc.GetString("station-ai-camera-map-crew-header"),
            HorizontalExpand = true,
        });
        var located = matches.Count(crew => crew.Position != null);
        header.AddChild(new Label
        {
            Text = Loc.GetString("station-ai-camera-map-crew-located", ("count", located)),
            ToolTip = Loc.GetString("station-ai-camera-map-crew-located-tooltip", ("located", located), ("total", matches.Count)),
            FontColorOverride = located > 0 ? ActiveColor : MutedColor,
        });
        AddTransient(header);

        var sprites = IoCManager.Resolve<IEntityManager>().System<SpriteSystem>();
        var prototypes = IoCManager.Resolve<IPrototypeManager>();
        foreach (var crew in matches)
            AddTransient(CrewRow(crew, sprites, prototypes));

        if (matches.Count == 0)
        {
            AddTransient(new Label
            {
                Text = Loc.GetString("station-ai-camera-map-crew-empty"),
                FontColorOverride = MutedColor,
                HorizontalAlignment = HAlignment.Center,
                Margin = new Thickness(0, 12),
            });
        }
    }

    /// <summary>Same layout and icon sizes as a crew monitoring console row.</summary>
    private Control CrewRow(StationAiCameraMapCrew crew, SpriteSystem sprites, IPrototypeManager prototypes)
    {
        var located = crew.Position != null;
        var (healthIcon, _) = StationAiCrewHealth.Get(crew.Alive, crew.Damage);
        var button = new ContainerButton
        {
            ToolTip = located
                ? $"{crew.Name}, {crew.Job}"
                : $"{crew.Name}, {crew.Job}\n{Loc.GetString("station-ai-camera-map-no-coordinates")}",
            Disabled = !located,
            HorizontalExpand = true,
            Margin = new Thickness(0, 1),
        };
        button.AddStyleClass(ContainerButton.StyleClassButton);
        var main = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, HorizontalExpand = true };
        button.AddChild(main);

        var status = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            SizeFlagsStretchRatio = 1.25f,
        };
        main.AddChild(status);
        // Coordinates indicator.
        status.AddChild(new TextureRect
        {
            Texture = _blip,
            TextureScale = new Vector2(0.25f, 0.25f),
            Modulate = located ? Color.LimeGreen : Color.DarkRed,
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
        });
        var health = new AnimatedTextureRect
        {
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            Margin = new Thickness(0, 1, 3, 0),
        };
        health.SetFromSpriteSpecifier(healthIcon);
        health.DisplayRect.TextureScale = new Vector2(2f, 2f);
        status.AddChild(health);
        status.AddChild(new Label { Text = crew.Name, HorizontalExpand = true, ClipText = true });

        var job = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, HorizontalExpand = true };
        main.AddChild(job);
        if (prototypes.TryIndex<JobIconPrototype>(crew.JobIcon, out var icon))
        {
            job.AddChild(new TextureRect
            {
                TextureScale = new Vector2(2f, 2f),
                VerticalAlignment = VAlignment.Center,
                Texture = sprites.Frame0(icon.Icon),
                Margin = new Thickness(5, 0, 5, 0),
            });
        }

        job.AddChild(new Label { Text = crew.Job, HorizontalExpand = true, ClipText = true });
        button.OnPressed += _ => SelectCrew(crew.Sensor);
        return button;
    }

    private void AddTransient(Control control)
    {
        _transient.Add(control);
        _cameraList.AddChild(control);
    }

    /// <summary>Reuses rows whose camera is unchanged and disposes the rest.</summary>
    private void SyncRows()
    {
        _cameraList.RemoveAllChildren();
        var rows = new Dictionary<(NetEntity Entity, string Name, bool Active), CameraRow>();
        foreach (var camera in _cameras)
        {
            if (!_rows.Remove(camera, out var row))
                row = new CameraRow(camera, _blip, ToggleFavorite, SelectTarget);
            rows[camera] = row;
        }

        foreach (var stale in _rows.Values)
            stale.Root.Dispose();
        _rows = rows;
    }

    private void ToggleFavorite(NetEntity camera)
    {
        if (!_favorites.Remove(camera))
            _favorites.Add(camera);
        RebuildCameraList();
    }

    /// <summary>Same look as a crew row: standard button, status dot, name, favourite star on the right.</summary>
    private sealed class CameraRow
    {
        public readonly ContainerButton Root;
        private readonly Button _pin;

        public CameraRow((NetEntity Entity, string Name, bool Active) camera, Texture blip,
            Action<NetEntity> toggleFavorite, Action<NetEntity> select)
        {
            var status = Loc.GetString(camera.Active ? "station-ai-camera-map-active" : "station-ai-camera-map-inactive");
            Root = new ContainerButton
            {
                ToolTip = $"{camera.Name} — {status}",
                Disabled = !camera.Active,
                HorizontalExpand = true,
                Margin = new Thickness(0, 1),
            };
            Root.AddStyleClass(ContainerButton.StyleClassButton);
            Root.OnPressed += _ => select(camera.Entity);
            var row = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal, HorizontalExpand = true };
            // Status dot, the same size as the crew monitor's coordinates indicator.
            row.AddChild(new TextureRect
            {
                Texture = blip,
                TextureScale = new Vector2(0.25f, 0.25f),
                Modulate = camera.Active ? Color.LimeGreen : Color.DarkRed,
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0),
            });
            row.AddChild(new Label
            {
                Text = camera.Name,
                ClipText = true,
                HorizontalExpand = true,
                VerticalAlignment = VAlignment.Center,
            });
            // The star stops the click, so pinning never jumps the view.
            _pin = new Button
            {
                SetWidth = 26,
                StyleBoxOverride = new StyleBoxFlat(Color.Transparent),
                VerticalAlignment = VAlignment.Center,
            };
            _pin.OnPressed += _ => toggleFavorite(camera.Entity);
            row.AddChild(_pin);
            Root.AddChild(row);
        }

        public void SetFavorite(bool favorite)
        {
            _pin.Text = favorite ? "★" : "☆";
            _pin.ToolTip = Loc.GetString(favorite ? "station-ai-camera-map-unpin" : "station-ai-camera-map-pin");
        }
    }

    /// <summary>
    /// The map shows only what the open tab can jump to, so camera and crew markers never overlap.
    /// </summary>
    private void RefreshBlips()
    {
        _map.TrackedEntities.Clear();
        _map.Names.Clear();
        _crewBlips.Clear();
        _inactiveBlips.Clear();
        if (_showCrew)
        {
            foreach (var crew in _crewState)
            {
                if (crew.Position is not { } position)
                    continue;
                _crewBlips.Add(crew.Sensor);
                _map.TrackedEntities[crew.Sensor] = new NavMapBlip(new EntityCoordinates(_grid, position),
                    _blip, StationAiCrewHealth.Get(crew.Alive, crew.Damage).Dot, false);
                _map.Names[crew.Sensor] = $"{crew.Name} ({crew.Job})";
            }

            return;
        }

        foreach (var target in _targets)
        {
            var color = target.IsCamera ? Color.Cyan : Color.Orange;
            var name = target.Name;
            if (!target.Active)
            {
                _inactiveBlips[target.Entity] = target.IsCamera;
                color = color.WithAlpha(0.35f) * Color.DimGray;
                name = Loc.GetString(target.IsCamera ? "station-ai-camera-map-blip-inactive" : "station-ai-camera-map-blip-uncovered",
                    ("name", target.Name));
            }

            _map.TrackedEntities[target.Entity] = new NavMapBlip(new EntityCoordinates(_grid, target.Position),
                _blip, color, false);
            _map.Names[target.Entity] = name;
        }
    }

    public void UpdateMap(EntityUid grid, StationAiCameraMapState state)
    {
        // A later cooldown than the one we knew means the server just moved the view.
        if (_hasState && state.NextTeleport > _nextTeleport)
            OnCameraSwitched?.Invoke();
        _hasState = true;
        _nextTeleport = state.NextTeleport;
        if (_map.MapUid != grid)
        {
            _map.MapUid = grid;
            _map.ForceNavMapUpdate();
        }

        _grid = grid;
        _targets = state.Targets;
        _crewState = state.Crew;
        RefreshBlips();

        var cameras = state.Targets.Where(target => target.IsCamera)
            .Select(target => (target.Entity, target.Name, target.Active))
            .OrderBy(camera => camera.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        var crewList = state.Crew
            .OrderBy(crew => crew.Position == null)
            .ThenBy(crew => crew.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
        var crewKeys = crewList
            .Select(crew => (crew.Sensor, crew.Name, crew.Job, crew.JobIcon,
                StationAiCrewHealth.Bucket(crew.Alive, crew.Damage), crew.Position != null))
            .ToList();
        _crew = crewList;
        // Keep buttons and scroll position stable across the server's periodic map refreshes.
        var crewChanged = !_crewKeys.SequenceEqual(crewKeys);
        _crewKeys = crewKeys;
        if (!_cameras.SequenceEqual(cameras))
        {
            _cameras = cameras;
            SyncRows();
            RebuildCameraList();
        }
        else if (crewChanged && _showCrew)
        {
            RebuildCameraList();
        }
    }
}
