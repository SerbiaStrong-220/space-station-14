// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Map;
using System.Numerics;
using static Content.Client.SS220.BoxLayout.BoxLayoutManager;

namespace Content.Client.SS220.BoxLayout;

public interface IBoxLayoutManager
{
    event Action? Started;
    event Action<BoxParams>? Ended;
    event Action? Cancelled;

    EntityUid? Parent { get; }
    Vector2? Point1 { get; }
    Vector2? Point2 { get; }
    Color Color { get; }
    bool Active { get; }
    bool AttachToGrid { get; set; }
    BoxParams? CurParams { get; }

    void Initialize();
    BoxParams? GetBoxParams();
    void Cancel();
    void StartNew();
    void SetColor(Color? newColor);
    void SetOverlay(bool enabled);
    MapCoordinates GetMouseMapCoordinates();
}
