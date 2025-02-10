// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using System.Numerics;
using Content.Shared.Maps;
using Robust.Client.Graphics;
using Robust.Client.Placement;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.SS220.MapEditor;

// Mostly clone of TileSpawningUIController with some spice into it.
public sealed class TileSpawningUIController220 : UIController
{
    [Dependency] private readonly IPlacementManager _placement = default!;
    [Dependency] private readonly IResourceCache _resources = default!;
    [Dependency] private readonly ITileDefinitionManager _tiles = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private TileSpawnWindow? _window;
    private bool _init;

    private readonly List<TileUIData> _shownTiles = new();
    private bool _clearingTileSelections;
    private bool _eraseTile;
    private uint _animationFrame = 0;
    private float _animationTimer = 0f;
    private float _animationFrameDelay = 0.5f;

    public override void Initialize()
    {
        DebugTools.Assert(_init == false);
        _init = true;
        _placement.PlacementChanged += ClearTileSelection;
    }

    private void StartTilePlacement(int tileType)
    {
        var newObjInfo = new PlacementInformation
        {
            PlacementOption = "AlignTileAny",
            TileType = tileType,
            Range = 400,
            IsTile = true
        };

        _placement.BeginPlacing(newObjInfo);
    }

    private void OnTileEraseToggled(ButtonToggledEventArgs args)
    {
        if (_window == null || _window.Disposed)
            return;

        _placement.Clear();

        if (args.Pressed)
        {
            _eraseTile = true;
            StartTilePlacement(0);
        }
        else
            _eraseTile = false;

        args.Button.Pressed = args.Pressed;
    }

    public void ToggleWindow()
    {
        EnsureWindow();

        if (_window!.IsOpen)
        {
            _window.Close();
        }
        else
        {
            _window.Open();
        }
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        // FUNNY ANIMATION CODE
        //              - stalengd
        _animationTimer += args.DeltaSeconds;
        if (_animationTimer >= _animationFrameDelay)
        {
            _animationTimer = 0;
            _animationFrame++;
            foreach (var tile in _shownTiles)
            {
                if (tile.Frames.Count < 2) continue;
                var frameIndex = (int)(_animationFrame % tile.Frames.Count);
                tile.ListItem.Icon = tile.Frames[frameIndex];
            }
        }
    }

    private void EnsureWindow()
    {
        if (_window is { Disposed: false })
            return;
        _window = UIManager.CreateWindow<TileSpawnWindow>();
        LayoutContainer.SetAnchorPreset(_window, LayoutContainer.LayoutPreset.CenterLeft);
        _window.SearchBar.GrabKeyboardFocus();
        _window.ClearButton.OnPressed += OnTileClearPressed;
        _window.SearchBar.OnTextChanged += OnTileSearchChanged;
        _window.TileList.OnItemSelected += OnTileItemSelected;
        _window.TileList.OnItemDeselected += OnTileItemDeselected;
        _window.EraseButton.Pressed = _eraseTile;
        _window.EraseButton.OnToggled += OnTileEraseToggled;
        BuildTileList();
    }

    public void CloseWindow()
    {
        if (_window == null || _window.Disposed) return;

        _window?.Close();
    }

    private void ClearTileSelection(object? sender, EventArgs e)
    {
        if (_window == null || _window.Disposed) return;
        _clearingTileSelections = true;
        _window.TileList.ClearSelected();
        _clearingTileSelections = false;
        _window.EraseButton.Pressed = false;
    }

    private void OnTileClearPressed(ButtonEventArgs args)
    {
        if (_window == null || _window.Disposed) return;

        _window.TileList.ClearSelected();
        _placement.Clear();
        _window.SearchBar.Clear();
        BuildTileList(string.Empty);
        _window.ClearButton.Disabled = true;
    }

    private void OnTileSearchChanged(LineEdit.LineEditEventArgs args)
    {
        if (_window == null || _window.Disposed) return;

        _window.TileList.ClearSelected();
        _placement.Clear();
        BuildTileList(args.Text);
        _window.ClearButton.Disabled = string.IsNullOrEmpty(args.Text);
    }

    private void OnTileItemSelected(ItemList.ItemListSelectedEventArgs args)
    {
        var definition = _shownTiles[args.ItemIndex];
        StartTilePlacement(definition.Tile.TileId);
    }

    private void OnTileItemDeselected(ItemList.ItemListDeselectedEventArgs args)
    {
        if (_clearingTileSelections)
        {
            return;
        }

        _placement.Clear();
    }

    private void BuildTileList(string? searchStr = null)
    {
        if (_window == null || _window.Disposed) return;

        _window.TileList.Clear();

        IEnumerable<TileUIData> tileDefs = _tiles.Where(def => !def.EditorHidden)
            .Select(def =>
            {
                string displayTitle = Loc.GetString(def.Name);
                if (_prototypeManager.TryGetMapping(typeof(ContentTileDefinition), def.ID, out var node)
                    && node.TryGet("tags", out ValueDataNode? valueNode))
                {
                    displayTitle = $"[{valueNode.Value}] {displayTitle}";
                }
                return new TileUIData(def, displayTitle, new ItemList.Item(_window.TileList), new List<Texture>());
            });

        if (!string.IsNullOrEmpty(searchStr))
        {
            tileDefs = tileDefs.Where(s =>
                s.DisplayTitle.Contains(searchStr, StringComparison.CurrentCultureIgnoreCase) ||
                s.Tile.ID.Contains(searchStr, StringComparison.OrdinalIgnoreCase));
        }

        tileDefs = tileDefs.OrderBy(d => d.DisplayTitle);

        _shownTiles.Clear();
        _shownTiles.AddRange(tileDefs);

        foreach (var entry in _shownTiles)
        {
            Texture? texture = null;
            var path = entry.Tile.Sprite?.ToString();

            if (path != null)
            {
                Texture baseTexture = _resources.GetResource<TextureResource>(path);
                for (var i = 0; i < baseTexture.Width / 32; i++)
                {
                    texture = new AtlasTexture(baseTexture,
                        UIBox2.FromDimensions(new Vector2(i * 32, 0), new Vector2(32, 32)));
                    entry.Frames.Add(texture);
                }
            }

            var displayTitle = entry.DisplayTitle;
            if (entry.Frames.Count > 1)
            {
                displayTitle = $"{displayTitle} [{entry.Frames.Count}]";
            }
            entry.ListItem.Text = displayTitle;
            entry.ListItem.Icon = texture;
            entry.ListItem.Selectable = true;
            entry.ListItem.IconRegion = UIBox2.FromDimensions(Vector2.Zero, new(32, 32));
            _window.TileList.Add(entry.ListItem);
        }
    }

    private record struct TileUIData(
        ITileDefinition Tile,
        string DisplayTitle,
        ItemList.Item ListItem,
        List<Texture> Frames);
}
