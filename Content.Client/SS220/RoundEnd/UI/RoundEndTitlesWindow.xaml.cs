// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Linq;
using System.Numerics;
using Content.Client.SS220.Shlepovend;
using Content.Shared.Roles;
using Content.Shared.SS220.Discord;
using Robust.Client.AutoGenerated;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Input;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

// Just to not type this abomination every time
using PlayerInfo = Content.Shared.GameTicking.RoundEndMessageEvent.RoundEndPlayerInfo;
using SponsorInfo = Content.Shared.GameTicking.RoundEndMessageEvent.RoundEndSponsorInfo;

namespace Content.Client.SS220.RoundEnd.UI;

[GenerateTypedNameReferences]
public sealed partial class RoundEndTitlesWindow : BaseWindow
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    private readonly ShlepovendSystem _shlepovendSystem = default!;
    private readonly RoundEndTitlesStyle _style;

    private float _timer = 0f;
    private bool _isAutoScrollEnabled = true;
    private bool _ignoreScrollEvent = false;

    private const float DOUBLE_CLICK_THRESHOLD = 0.2f;
    private const int DRAG_MARGIN_SIZE = 7;
    private const string ANIMATION_KEY_FADE_IN = "FadeIn";
    private const int SPONSOR_GRID_COLUMNS = 3;

    // Double click functionality... not the best place for it, but anyway
    private float _clickTime = 0f;
    private int _clickCount = 0;

    private bool _isMaximazed;
    private Vector2 _defaultSize;

    private bool IsMaximazed
    {
        get => _isMaximazed;
        set
        {
            _isMaximazed = value;
            ToggleFullscreenButton.Pressed = value;
            ToggleFullscreenButton.Text = Loc.GetString(!value ? "round-end-titles-set-fullscreen-on" : "round-end-titles-set-fullscreen-off");
        }
    }
    private bool IsAutoScrollEnabled
    {
        get => _isAutoScrollEnabled;
        set
        {
            _isAutoScrollEnabled = value;
            AutoScrollCheckBox.Pressed = value;
        }
    }

    public RoundEndTitlesWindow()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        _shlepovendSystem = _entityManager.System<ShlepovendSystem>();
        _style = new();
        Stylesheet = _style.Create(UserInterfaceManager.Stylesheet, _resourceCache);
    }

    public RoundEndTitlesWindow(
        string gamemode,
        string roundEnd,
        TimeSpan roundTimeSpan,
        int roundId,
        PlayerInfo[] players,
        SponsorInfo[] sponsors) : this()
    {

        RoundNumberLabel.Text = Loc.GetString("round-end-titles-round-id-template", ("roundId", roundId));
        RoundGamemodeLabel.Text = Loc.GetString("round-end-titles-gamemode-template", ("gamemode", Loc.GetString(gamemode)));

        // Refresh checkbox
        IsAutoScrollEnabled = IsAutoScrollEnabled;

        _defaultSize = SetSize;

        TitlesScroll.OnMouseWheel += _ =>
        {
            IsAutoScrollEnabled = false;
        };
        if (TitlesScroll.GetChild(1) is VScrollBar vScrollBar)
        {
            vScrollBar.OnValueChanged += _ =>
            {
                if (_ignoreScrollEvent)
                    return;
                if (_timer < 0.1f) // Ahh...
                    return;
                IsAutoScrollEnabled = false;
            };
        }
        AutoScrollCheckBox.OnPressed += args =>
        {
            IsAutoScrollEnabled = args.Button.Pressed;
        };
        ToggleFullscreenButton.OnPressed += args =>
        {
            ToggleMaximize();
        };
        CloseButton.OnPressed += _ => Close();

        PlayFadeIn();

        DisplayAntags(players);
        DisplayWorkers(players);
        DisplayObservers(players);
        DisplaySponsors(sponsors);
    }

    public void Maximaze()
    {
        IsMaximazed = true;
        SetSize = Parent!.Size;
        RecenterWindow(new(0f, 0f));
    }

    public void Minimize()
    {
        IsMaximazed = false;
        SetSize = _defaultSize;
        RecenterWindow(new(0.1f, 0.1f));
    }

    public void ToggleMaximize()
    {
        if (IsMaximazed)
            Minimize();
        else
            Maximaze();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        _timer += args.DeltaSeconds;
        if (_isAutoScrollEnabled)
        {
            var scroll = TitlesScroll.GetScrollValue();
            var scrollSpeed = _style.GetScrollingSpeed(TimeSpan.FromSeconds(_timer));
            _ignoreScrollEvent = true;
            TitlesScroll.SetScrollValue(scroll + new Vector2(0f, scrollSpeed * args.DeltaSeconds));
            _ignoreScrollEvent = false;
        }
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);
        if (args.Function == EngineKeyFunctions.UIClick)
        {
            var clickTimeDelta = _timer - _clickTime;
            if (clickTimeDelta > DOUBLE_CLICK_THRESHOLD)
            {
                _clickCount = 0;
            }
            _clickTime = _timer;
            _clickCount++;
            if (_clickCount == 2)
            {
                OnDoubleClick();
            }
        }
    }

    protected override DragMode GetDragModeFor(Vector2 relativeMousePos)
    {
        if (IsMaximazed)
            return DragMode.None;
        var mode = DragMode.Move;

        if (relativeMousePos.Y < DRAG_MARGIN_SIZE)
        {
            mode = DragMode.Top;
        }
        else if (relativeMousePos.Y > Size.Y - DRAG_MARGIN_SIZE)
        {
            mode = DragMode.Bottom;
        }
        if (relativeMousePos.X < DRAG_MARGIN_SIZE)
        {
            mode |= DragMode.Left;
        }
        else if (relativeMousePos.X > Size.X - DRAG_MARGIN_SIZE)
        {
            mode |= DragMode.Right;
        }

        return mode;
    }

    private void OnDoubleClick()
    {
        ToggleMaximize();
    }

    private void PlayFadeIn()
    {
        PlayAnimation(_style.FadeInAnimation, ANIMATION_KEY_FADE_IN);
    }

    private void DisplayAntags(IReadOnlyList<PlayerInfo> players)
    {
        var antagsList = new List<AntagInfo>();
        foreach (var player in players)
        {
            foreach (var antagPrototypeId in player.AntagPrototypes)
            {
                if (_prototypeManager.TryIndex<AntagPrototype>(antagPrototypeId, out var antagPrototype))
                    antagsList.Add(new(player, antagPrototype));
            }
        }
        CreateAntagsSection(antagsList);
    }

    private void CreateAntagsSection(IReadOnlyList<AntagInfo> antags)
    {
        if (antags.Count == 0)
            return;
        var section = new RoundEndTitlesSection(Loc.GetString("round-end-titles-antag-section"),
            _style.GetAntagIcon());
        DepartmentsContainer.AddChild(section);
        var i = 0;
        foreach (var antag in antags)
        {
            var role = new RoundEndTitlesRole(antag.Player.PlayerOOCName,
                antag.Player.PlayerICName ?? "",
                Loc.GetString(antag.Antag.Name),
                i,
                antag.Antag.AntagColor,
                antag.Player.PlayerNetEntity);
            section.RolesContainer.AddChild(role);
            i++;
        }
    }

    private void DisplayWorkers(IReadOnlyList<PlayerInfo> players)
    {
        var departments = _prototypeManager.GetInstances<DepartmentPrototype>();
        var jobs = _prototypeManager.GetInstances<JobPrototype>();

        var jobColors = new Dictionary<JobPrototype, Color>();
        var departmentsWorkers = new Dictionary<DepartmentPrototype, List<WorkerInfo>>();
        foreach (var (_, department) in departments.OrderBy(x => x.Value.Sort))
        {
            var departmentWorkers = new List<WorkerInfo>();
            departmentsWorkers.Add(department, departmentWorkers);
            foreach (var jobPrototypeId in department.Roles)
            {
                var job = jobs[jobPrototypeId];
                jobColors.TryAdd(job, department.Color);
                foreach (var player in players)
                {
                    if (!player.JobPrototypes.Contains(jobPrototypeId.Id))
                        continue;
                    departmentWorkers.Add(new(player, job));
                }
            }
            departmentWorkers.Sort((a, b) => b.Job.RealDisplayWeight.CompareTo(a.Job.RealDisplayWeight));
        }
        foreach (var (department, workers) in departmentsWorkers.OrderByDescending(x => x.Key.Weight))
        {
            CreateDepartment(department, workers, jobColors);
        }
    }

    private void CreateDepartment(DepartmentPrototype department, IReadOnlyList<WorkerInfo> workers, Dictionary<JobPrototype, Color> colors)
    {
        if (workers.Count == 0)
            return;
        var section = new RoundEndTitlesSection(Loc.GetString(department.Name), _style.GetDepartmentIcon(department.ID));
        DepartmentsContainer.AddChild(section);
        var i = 0;
        foreach (var worker in workers)
        {
            var role = new RoundEndTitlesRole(worker.Player.PlayerOOCName,
                worker.Player.PlayerICName ?? "",
                worker.Job.LocalizedName,
                i,
                colors.GetValueOrDefault(worker.Job, Color.White),
                worker.Player.PlayerNetEntity);
            section.RolesContainer.AddChild(role);
            i++;
        }
    }

    private void DisplayObservers(IReadOnlyList<PlayerInfo> players)
    {
        var observers = new List<PlayerInfo>();
        foreach (var player in players)
        {
            if (player.Observer)
                observers.Add(player);
        }
        CreateObserversSection(observers);
    }

    private void CreateObserversSection(IReadOnlyList<PlayerInfo> observers)
    {
        if (observers.Count == 0)
            return;
        var section = new RoundEndTitlesSection(Loc.GetString("round-end-titles-observers-section"),
            null);
        DepartmentsContainer.AddChild(section);
        var i = 0;
        foreach (var observer in observers)
        {
            var role = new RoundEndTitlesRole(observer.PlayerOOCName,
                observer.PlayerICName ?? "",
                Loc.GetString("round-end-titles-observer-role"),
                i,
                Color.White,
                observer.PlayerNetEntity);
            section.RolesContainer.AddChild(role);
            i++;
        }
    }

    private void DisplaySponsors(IReadOnlyList<SponsorInfo> sponsors)
    {
        var tiersToDisplay = new[]
        {
            SponsorTier.CriticalMassShlopa,
            SponsorTier.GoldenShlopa,
            SponsorTier.HugeShlopa,
        };

        var sponsorGroups = new Dictionary<SponsorTier, List<string>>();
        for (var i = 0; i < tiersToDisplay.Length; i++)
        {
            sponsorGroups.Add(tiersToDisplay[i], new());
        }
        for (var i = 0; i < sponsors.Count; i++)
        {
            var s = sponsors[i];
            for (var j = 0; j < s.Tiers.Length; j++)
            {
                if (sponsorGroups.TryGetValue(s.Tiers[j], out var group))
                {
                    group.Add(s.PlayerOOCName);
                }
            }
        }
        for (var i = 0; i < tiersToDisplay.Length; i++)
        {
            var tier = tiersToDisplay[i];
            if (!sponsorGroups.TryGetValue(tier, out var group))
                continue;
            group.Sort();
            CreateSponsorGroup(tier, group);
        }

    }

    private void CreateSponsorGroup(SponsorTier tier, IReadOnlyList<string> users)
    {
        if (users.Count == 0)
            return;
        var tierProto = _shlepovendSystem.GetTierRewardOrDefault(tier);
        if (tierProto == null)
            return;
        var columns = Math.Min(users.Count, SPONSOR_GRID_COLUMNS);
        var section = new RoundEndTitlesSection(tierProto.Name, null, columns);
        SponsorsContainer.AddChild(section);
        foreach (var user in users)
        {
            var userContainer = new Control()
            {
                HorizontalExpand = true,
            };
            var userLabel = new Label()
            {
                SetWidth = 0,
                Text = user,
                Align = Label.AlignMode.Center,
                HorizontalAlignment = HAlignment.Center,
                StyleClasses = { "RoundEndTitlesSponsorName" },
            };
            userContainer.AddChild(userLabel);
            section.RolesContainer.AddChild(userContainer);
        }
    }

    private readonly record struct WorkerInfo(PlayerInfo Player, JobPrototype Job) { }
    private readonly record struct AntagInfo(PlayerInfo Player, AntagPrototype Antag) { }
}

public sealed class RoundEndTitlesWindowScrollContainer : ScrollContainer
{
    public event Action<GUIMouseWheelEventArgs>? OnMouseWheel;

    protected override void MouseWheel(GUIMouseWheelEventArgs args)
    {
        base.MouseWheel(args);
        OnMouseWheel?.Invoke(args);
    }
}