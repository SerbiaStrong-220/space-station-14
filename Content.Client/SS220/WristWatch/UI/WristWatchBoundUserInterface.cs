// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.GameTicking.Managers;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.SS220.WristWatch.UI;

[UsedImplicitly]
public sealed class WristWatchBoundUserInterface : BoundUserInterface
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    private readonly ClientGameTicker _gameTicker = default!;

    [ViewVariables]
    private WristWatchMenu? _menu;
    [ViewVariables]
    private WristWatchStylePrototype? _style;


    public WristWatchBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _gameTicker = _entityManager.System<ClientGameTicker>();
    }

    public void RefreshStyle()
    {
        RetrieveStyleFrom(Owner);
        TryApplyStyle();
    }

    public void RequestUpdate()
    {
        var stationTime = _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan);
        _menu?.SetTime(stationTime);
    }

    protected override void Open()
    {
        base.Open();

        _menu = new(this);
        _menu.OpenCentered();
        RequestUpdate();
        // Turns out, I should pull style from there, otherwise its not working properly
        RefreshStyle();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;
        _menu?.Close();
    }

    private bool TryApplyStyle()
    {
        if (_menu == null || _style == null)
        {
            return false;
        }
        _menu.SetStyle(_style);
        return true;
    }

    private WristWatchStylePrototype? GetStyle(string prototypeId)
    {
        _prototypeManager.TryIndex<WristWatchStylePrototype>(prototypeId, out var style);
        return style;
    }

    private void RetrieveStyleFrom(EntityUid entity)
    {
        if (!_entityManager.TryGetComponent<WristWatchComponent?>(entity, out var comp))
            return;
        var style = GetStyle(comp.Style);
        if (style == null)
            return;
        _style = style;
    }
}
