// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using System.Numerics;
using Content.Client.Pinpointer.UI;
using Content.Shared.SS220.CultYogg.CultMiniMap;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client.SS220.CultYogg.CultMiniMap;

public sealed partial class CultMiniMapNavMapControl : NavMapControl
{
    // Keep the same base palette as the standard crew-monitoring map.
    private static readonly Color DefaultWallColor = new(192, 122, 196);
    private static readonly Color DefaultTileColor = new(71, 42, 72);

    public NetEntity? Focus;
    public readonly Dictionary<NetEntity, string> LocalizedNames = new();
    public readonly Dictionary<NetEntity, CultMiniMapStructureBlip> StructureMarkers = new();
    public readonly Dictionary<uint, CultMiniMapPingBlip> Pings = new();
    public bool PingMode;
    public event Action<EntityCoordinates>? PingRequestedAction;

    private readonly Label _trackedEntityLabel;
    private readonly PanelContainer _trackedEntityPanel;

    public CultMiniMapNavMapControl()
    {
        WallColor = DefaultWallColor;
        TileColor = DefaultTileColor;
        BackgroundColor = Color.FromSrgb(TileColor.WithAlpha(BackgroundOpacity));

        _trackedEntityLabel = new Label
        {
            Margin = new Thickness(10f, 8f),
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            Modulate = Color.White,
        };

        _trackedEntityPanel = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = BackgroundColor,
            },

            Margin = new Thickness(5f, 10f),
            HorizontalAlignment = HAlignment.Left,
            VerticalAlignment = VAlignment.Bottom,
            Visible = false,
        };

        _trackedEntityPanel.AddChild(_trackedEntityLabel);
        AddChild(_trackedEntityPanel);
        PostWallDrawingAction += DrawOverlays;
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        if (!PingMode || args.Function != EngineKeyFunctions.UIClick)
        {
            base.KeyBindUp(args);
            return;
        }

        var dragDistance = (StartDragPosition - args.PointerLocation.Position).Length();
        if (dragDistance > MinDragDistance || MapUid is not { } grid)
        {
            base.KeyBindUp(args);
            return;
        }

        // InverseMapPosition accounts for zoom and user panning; NavMap's offset also includes
        // the grid's physics center, which must be restored for grid-local coordinates.
        var position = InverseMapPosition(args.RelativePixelPosition) + GetOffset() - Offset;
        PingMode = false;
        PingRequestedAction?.Invoke(new EntityCoordinates(grid, position));
        args.Handle();
    }

    private void DrawOverlays(DrawingHandleScreen handle)
    {
        DrawStructures(handle);
        DrawPings(handle);
    }

    private void DrawStructures(DrawingHandleScreen handle)
    {
        foreach (var structure in StructureMarkers.Values)
        {
            switch (structure.MarkerType)
            {
                case CultMiniMapMarkerType.Wall:
                    DrawWall(handle, structure);
                    break;
                case CultMiniMapMarkerType.SecretDoor:
                    DrawSecretDoor(handle, structure);
                    break;
                case CultMiniMapMarkerType.Airlock:
                    DrawAirlock(handle, structure);
                    break;
            }
        }
    }

    private void DrawWall(DrawingHandleScreen handle, CultMiniMapStructureBlip structure)
    {
        var topLeft = StructurePoint(structure, new Vector2(-0.5f, 0.5f));
        var topRight = StructurePoint(structure, new Vector2(0.5f, 0.5f));
        var bottomRight = StructurePoint(structure, new Vector2(0.5f, -0.5f));
        var bottomLeft = StructurePoint(structure, new Vector2(-0.5f, -0.5f));

        if (!structure.Neighbors.HasFlag(CultMiniMapStructureNeighbors.North))
            handle.DrawLine(topLeft, topRight, structure.Color);
        if (!structure.Neighbors.HasFlag(CultMiniMapStructureNeighbors.East))
            handle.DrawLine(topRight, bottomRight, structure.Color);
        if (!structure.Neighbors.HasFlag(CultMiniMapStructureNeighbors.South))
            handle.DrawLine(bottomRight, bottomLeft, structure.Color);
        if (!structure.Neighbors.HasFlag(CultMiniMapStructureNeighbors.West))
            handle.DrawLine(bottomLeft, topLeft, structure.Color);

        handle.DrawLine(topLeft, bottomRight, structure.Color);
    }

    private void DrawSecretDoor(DrawingHandleScreen handle, CultMiniMapStructureBlip structure)
    {
        DrawWall(handle, structure);
        handle.DrawLine(
            StructurePoint(structure, new Vector2(0.5f, 0.5f)),
            StructurePoint(structure, new Vector2(-0.5f, -0.5f)),
            structure.Color);
    }

    internal static CultMiniMapStructureNeighbors GetStructureNeighbors(
        HashSet<CultMiniMapStructureLocation> walls,
        CultMiniMapStructureLocation location)
    {
        var neighbors = CultMiniMapStructureNeighbors.None;
        if (walls.Contains(location with { Tile = location.Tile + Vector2i.Up }))
            neighbors |= CultMiniMapStructureNeighbors.North;
        if (walls.Contains(location with { Tile = location.Tile + Vector2i.Right }))
            neighbors |= CultMiniMapStructureNeighbors.East;
        if (walls.Contains(location with { Tile = location.Tile + Vector2i.Down }))
            neighbors |= CultMiniMapStructureNeighbors.South;
        if (walls.Contains(location with { Tile = location.Tile + Vector2i.Left }))
            neighbors |= CultMiniMapStructureNeighbors.West;
        return neighbors;
    }

    internal void UpdateStructureNeighbors()
    {
        var connectedWallLocations = StructureMarkers.Values
            .Where(IsConnectedWall)
            .Select(marker => marker.Location)
            .OfType<CultMiniMapStructureLocation>()
            .ToHashSet();

        foreach (var (entity, marker) in StructureMarkers.ToArray())
        {
            if (!IsConnectedWall(marker) || marker.Location is not { } location)
                continue;

            StructureMarkers[entity] = marker with
            {
                Neighbors = GetStructureNeighbors(connectedWallLocations, location),
            };
        }
    }

    private static bool IsConnectedWall(CultMiniMapStructureBlip marker)
    {
        return marker.MarkerType is CultMiniMapMarkerType.Wall or CultMiniMapMarkerType.SecretDoor;
    }

    private void DrawAirlock(DrawingHandleScreen handle, CultMiniMapStructureBlip structure)
    {
        var extent = 0.5f - FullWallInstep;
        var topLeft = StructurePoint(structure, new Vector2(-extent, extent));
        var topRight = StructurePoint(structure, new Vector2(extent, extent));
        var bottomRight = StructurePoint(structure, new Vector2(extent, -extent));
        var bottomLeft = StructurePoint(structure, new Vector2(-extent, -extent));

        DrawRectangle(handle, topLeft, topRight, bottomRight, bottomLeft, structure.Color);
        handle.DrawLine(
            StructurePoint(structure, new Vector2(0f, extent)),
            StructurePoint(structure, new Vector2(0f, -extent)),
            structure.Color);
    }

    private static void DrawRectangle(
        DrawingHandleScreen handle,
        Vector2 topLeft,
        Vector2 topRight,
        Vector2 bottomRight,
        Vector2 bottomLeft,
        Color color)
    {
        handle.DrawLine(topLeft, topRight, color);
        handle.DrawLine(topRight, bottomRight, color);
        handle.DrawLine(bottomRight, bottomLeft, color);
        handle.DrawLine(bottomLeft, topLeft, color);
    }

    private Vector2 StructurePoint(CultMiniMapStructureBlip structure, Vector2 offset)
    {
        var rotated = new Angle(structure.Rotation).RotateVec(offset);
        var local = structure.Coordinates.Position + rotated - GetOffset();
        return ScalePosition(new Vector2(local.X, -local.Y));
    }

    private void DrawPings(DrawingHandleScreen handle)
    {
        var seconds = (float) Timing.RealTime.TotalSeconds;
        var offset = GetOffset();
        var mapScale = MinmapScaleModifier * MathF.Sqrt(MinimapScale);
        foreach (var ping in Pings.Values)
        {
            var local = ping.Coordinates.Position - offset;
            var position = ScalePosition(new Vector2(local.X, -local.Y));
            var phase = (seconds * 0.85f + ping.Id * 0.173f) % 1f;
            DrawPingRing(handle, position, ping.Color, phase);
            DrawPingRing(handle, position, ping.Color, (phase + 0.5f) % 1f);

            var heartbeat = 1f + 0.08f * MathF.Sin(seconds * MathF.Tau * 2f + ping.Id);
            var coefficient = mapScale * ping.Scale * heartbeat;
            var extent = new Vector2(coefficient * ping.Texture.Width, coefficient * ping.Texture.Height);
            handle.DrawTextureRect(ping.Texture, new UIBox2(position - extent, position + extent), ping.Color);
        }
    }

    private static void DrawPingRing(DrawingHandleScreen handle, Vector2 position, Color color, float phase)
    {
        var radius = 6f + phase * 24f;
        var alpha = 0.85f * (1f - phase);
        handle.DrawCircle(position, radius, color.WithAlpha(alpha), false);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (Focus is not { } focus || !TrackedEntities.TryGetValue(focus, out var blip))
        {
            HideTrackedEntity();
            return;
        }

        if (!LocalizedNames.TryGetValue(focus, out var name))
            name = Loc.GetString("navmap-unknown-entity");

        _trackedEntityLabel.Text = name + "\n" + Loc.GetString("navmap-location",
            ("x", MathF.Round(blip.Coordinates.X)),
            ("y", MathF.Round(blip.Coordinates.Y)));
        _trackedEntityPanel.Visible = true;
    }

    private void HideTrackedEntity()
    {
        _trackedEntityLabel.Text = string.Empty;
        _trackedEntityPanel.Visible = false;
    }
}

public readonly record struct CultMiniMapPingBlip(
    uint Id,
    EntityCoordinates Coordinates,
    Texture Texture,
    Color Color,
    float Scale);

public readonly record struct CultMiniMapStructureBlip(
    EntityCoordinates Coordinates,
    CultMiniMapMarkerType MarkerType,
    Color Color,
    float Rotation,
    CultMiniMapStructureLocation? Location,
    CultMiniMapStructureNeighbors Neighbors);
