// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Map;
using System.Numerics;
using static Content.Client.SS220.BoxLayout.BoxLayoutManager;

namespace Content.Client.SS220.BoxLayout;

public interface IBoxLayoutManager
{
    event Action? Started;
    event Action<BoxArgs>? Ended;
    event Action? Cancelled;

    bool Active { get; }

    EntityCoordinates? FirstPoint { get; }

    Color? OverlayOverrideColor { get; set; }

    bool AttachToGrid { get; set; }

    void Initialize();

    void StartNew();

    void Cancel();

    void SetOverlay(bool enabled);
}
