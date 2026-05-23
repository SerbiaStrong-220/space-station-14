// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using System.Numerics;
using Content.Shared.SS220.Arena.Lobby;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.SS220.Arena.Lobby;

public sealed class ArenaLobbyWindow : DefaultWindow
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private const string CategoryAll = "";

    public event Action<string>? OnCreateRequested;
    public event Action<uint>? OnJoinRequested;
    public event Action? OnRefreshRequested;

    private readonly Label _counter;
    private readonly Label _cooldownLabel;
    private readonly BoxContainer _categoriesBar;
    private readonly BoxContainer _arenasList;
    private readonly BoxContainer _templatesList;
    private readonly Button _refreshButton;

    private readonly List<ArenaPrototype> _templates = new();

    private static readonly Color ColorWaiting = Color.FromHex("#5dadd8");
    private static readonly Color ColorCountdown = Color.FromHex("#e0c46d");
    private static readonly Color ColorFighting = Color.FromHex("#e06b5d");
    private static readonly Color ColorFinished = Color.FromHex("#888888");

    private string _selectedCategory = CategoryAll;
    private ArenaLobbyEuiState? _lastState;
    private List<string> _knownCategories = new();

    public ArenaLobbyWindow()
    {
        IoCManager.InjectDependencies(this);
        ReloadTemplates();

        Title = Loc.GetString("arena-lobby-title");
        MinSize = new Vector2(520, 520);
        SetSize = new Vector2(560, 600);

        _counter = new Label
        {
            HorizontalExpand = true,
            HorizontalAlignment = HAlignment.Right,
            StyleClasses = { "LabelSubText" },
        };

        _cooldownLabel = new Label
        {
            StyleClasses = { "LabelSubText" },
            FontColorOverride = Color.FromHex("#e0c46d"),
            Visible = false,
            Margin = new Thickness(2, 0, 0, 0),
        };

        _refreshButton = new Button
        {
            Text = Loc.GetString("arena-lobby-refresh"),
            HorizontalAlignment = HAlignment.Right,
        };
        _refreshButton.OnPressed += _ => OnRefreshRequested?.Invoke();

        _categoriesBar = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 4,
            Margin = new Thickness(0, 2, 0, 2),
        };

        _arenasList = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            VerticalExpand = true,
            SeparationOverride = 4,
        };

        _templatesList = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 4,
        };

        var arenasScroll = new ScrollContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            HScrollEnabled = false,
            Children = { _arenasList },
        };

        var templatesScroll = new ScrollContainer
        {
            HorizontalExpand = true,
            HScrollEnabled = false,
            MinSize = new Vector2(0, 180),
            Children = { _templatesList },
        };

        ContentsContainer.AddChild(new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
            SeparationOverride = 6,
            Children =
            {
                _categoriesBar,
                new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    Children =
                    {
                        SectionHeading("arena-lobby-existing"),
                        _counter,
                    },
                },
                new PanelContainer { StyleClasses = { "LowDivider" } },
                arenasScroll,
                SectionHeading("arena-lobby-create-header"),
                _cooldownLabel,
                new PanelContainer { StyleClasses = { "LowDivider" } },
                templatesScroll,
                _refreshButton,
            },
        });
    }

    private static Label SectionHeading(string locKey)
    {
        return new Label
        {
            Text = Loc.GetString(locKey),
            StyleClasses = { "LabelKeyText" },
            HorizontalExpand = true,
            Margin = new Thickness(2, 4, 0, 0),
        };
    }

    public void Update(ArenaLobbyEuiState state)
    {
        _lastState = state;
        RebuildCategories(state);
        RebuildLists(state);
    }

    private void ReloadTemplates()
    {
        _templates.Clear();
        foreach (var proto in _proto.EnumeratePrototypes<ArenaPrototype>())
            _templates.Add(proto);
        _templates.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
    }

    private void RebuildCategories(ArenaLobbyEuiState state)
    {
        var categories = _templates.Select(t => t.Category)
            .Concat(state.Arenas.Select(a => a.Category))
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct()
            .OrderBy(c => c)
            .ToList();

        if (_selectedCategory != CategoryAll && !categories.Contains(_selectedCategory))
            _selectedCategory = CategoryAll;

        if (_knownCategories.SequenceEqual(categories))
            return;
        _knownCategories = categories;

        _categoriesBar.RemoveAllChildren();
        _categoriesBar.AddChild(BuildCategoryButton(CategoryAll, "arena-lobby-category-all"));
        foreach (var cat in categories)
            _categoriesBar.AddChild(BuildCategoryButton(cat, $"arena-lobby-category-{cat}"));
    }

    private Button BuildCategoryButton(string category, string locKey)
    {
        var label = Loc.TryGetString(locKey, out var text) ? text : category;
        var button = new Button
        {
            Text = label,
            ToggleMode = true,
            Pressed = _selectedCategory == category,
        };
        button.OnPressed += _ =>
        {
            _selectedCategory = category;
            if (_lastState != null)
                RebuildLists(_lastState);
            foreach (var child in _categoriesBar.Children)
            {
                if (child is Button b)
                    b.Pressed = ReferenceEquals(b, button);
            }
        };
        return button;
    }

    private void RebuildLists(ArenaLobbyEuiState state)
    {
        _counter.Text = Loc.GetString("arena-lobby-header",
            ("count", state.ActiveCount),
            ("max", state.MaxArenas));

        if (state.CreateCooldownRemaining > 0)
        {
            _cooldownLabel.Visible = true;
            _cooldownLabel.Text = Loc.GetString("arena-lobby-cooldown", ("cooldown", state.CreateCooldownRemaining));
        }
        else
        {
            _cooldownLabel.Visible = false;
        }

        _arenasList.RemoveAllChildren();
        var filteredArenas = state.Arenas.Where(MatchesCategory).ToList();
        if (filteredArenas.Count == 0)
        {
            _arenasList.AddChild(new Label
            {
                Text = Loc.GetString("arena-lobby-no-arenas"),
                StyleClasses = { "LabelSubText" },
                HorizontalAlignment = HAlignment.Center,
                Margin = new Thickness(0, 18, 0, 0),
            });
        }
        else
        {
            foreach (var entry in filteredArenas)
                _arenasList.AddChild(BuildArenaRow(entry));
        }

        _templatesList.RemoveAllChildren();
        var canCreate = state.ActiveCount < state.MaxArenas && !state.HasOwnArena && state.CreateCooldownRemaining <= 0;
        foreach (var tmpl in _templates.Where(MatchesCategory))
            _templatesList.AddChild(BuildTemplateRow(tmpl, canCreate));
    }

    private bool MatchesCategory(ArenaLobbyEntry entry) => _selectedCategory == CategoryAll || entry.Category == _selectedCategory;
    private bool MatchesCategory(ArenaPrototype tmpl) => _selectedCategory == CategoryAll || tmpl.Category == _selectedCategory;

    private Control BuildArenaRow(ArenaLobbyEntry entry)
    {
        var (statusKey, statusColor) = entry.Status switch
        {
            ArenaLobbyStatus.Waiting => ("arena-lobby-status-waiting", ColorWaiting),
            ArenaLobbyStatus.Countdown => ("arena-lobby-status-countdown", ColorCountdown),
            ArenaLobbyStatus.Fighting => ("arena-lobby-status-fighting", ColorFighting),
            _ => ("arena-lobby-status-finished", ColorFinished),
        };

        var name = new Label
        {
            Text = entry.Name,
            StyleClasses = { "LabelKeyText" },
        };

        var status = new Label
        {
            Text = $"[{Loc.GetString(statusKey)}]",
            FontColorOverride = statusColor,
        };

        var players = new Label
        {
            Text = $"{entry.Players}/{entry.MaxPlayers}",
            StyleClasses = { "LabelSubText" },
            HorizontalAlignment = HAlignment.Right,
            HorizontalExpand = true,
            Margin = new Thickness(0, 0, 8, 0),
        };

        var join = new Button
        {
            Text = Loc.GetString("arena-lobby-join"),
            Disabled = entry.Status == ArenaLobbyStatus.Finished || entry.Players >= entry.MaxPlayers,
            MinSize = new Vector2(86, 0),
        };
        var id = entry.ArenaId;
        join.OnPressed += _ => OnJoinRequested?.Invoke(id);

        var row = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            VerticalAlignment = VAlignment.Center,
            SeparationOverride = 8,
            Margin = new Thickness(8, 6),
            Children = { name, status, players, join },
        };

        return new PanelContainer
        {
            StyleClasses = { "AngleRect" },
            HorizontalExpand = true,
            Children = { row },
        };
    }

    private Control BuildTemplateRow(ArenaPrototype tmpl, bool canCreate)
    {
        var name = new Label
        {
            Text = tmpl.Name,
            StyleClasses = { "LabelKeyText" },
            HorizontalExpand = true,
        };

        var size = new Label
        {
            Text = Loc.GetString("arena-lobby-template-size", ("count", tmpl.MaxPlayers)),
            StyleClasses = { "LabelSubText" },
            Margin = new Thickness(0, 0, 8, 0),
        };

        var create = new Button
        {
            Text = Loc.GetString("arena-lobby-create"),
            Disabled = !canCreate,
            MinSize = new Vector2(86, 0),
        };
        var protoId = tmpl.ID;
        create.OnPressed += _ => OnCreateRequested?.Invoke(protoId);

        var top = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            VerticalAlignment = VAlignment.Center,
            SeparationOverride = 8,
            Children = { name, size, create },
        };

        var body = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            Margin = new Thickness(8, 6),
            SeparationOverride = 2,
            Children = { top },
        };

        if (!string.IsNullOrWhiteSpace(tmpl.Description))
        {
            body.AddChild(new Label
            {
                Text = tmpl.Description,
                StyleClasses = { "LabelSubText" },
                FontColorOverride = Color.FromHex("#888888"),
            });
        }

        return new PanelContainer
        {
            StyleClasses = { "AngleRect" },
            HorizontalExpand = true,
            Children = { body },
        };
    }
}
