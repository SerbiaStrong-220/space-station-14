

using Robust.Shared.Map;
using static Content.Client.SS220.BoxLayout.BoxLayoutManager;

namespace Content.Client.SS220.BoxLayout;

public interface IBoxLayoutManager
{
    event Action? Started;
    event Action<BoxParams>? Ended;
    event Action? Cancelled;

    bool Active { get; }
    BoxParams? CurParams { get; }

    void Initialize();
    BoxParams? GetBoxParams();
    void Cancel();
    void StartNewBox();
    void SetColor(Color? newColor);
    void SetOverlay(bool enabled);
    MapCoordinates GetMouseMapCoordinates();
}
