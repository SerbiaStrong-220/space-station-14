using Content.Server.GameTicking;
using Content.Shared.Maps;
using Content.Shared.SS220.CCVars;
using Robust.Shared.Prototypes;

namespace Content.Server.Maps;

public partial class GameMapManager
{
    private GameTicker _gameTicker = default!;

    [ViewVariables(VVAccess.ReadOnly)]
    private bool _playedMapMemoryEnabled = false;
    [ViewVariables(VVAccess.ReadOnly)]
    private int _playedMapQueueDepth = 1;
    [ViewVariables(VVAccess.ReadOnly)]
    private readonly Queue<ProtoId<GameMapPrototype>> _playedMapQueue = new();

    private void InitilizeSS220()
    {
        _gameTicker = _entityManager.System<GameTicker>();
        _gameTicker.OnMainStationMapLoaded += OnMainStationMapLoaded;

        _configurationManager.OnValueChanged(CCVars220.GamePlayedMapMemory, value =>
        {
            _playedMapMemoryEnabled = value;
            if (!_playedMapMemoryEnabled)
                _playedMapQueue.Clear();
        }, true);
        _configurationManager.OnValueChanged(CCVars220.GamePlayedMapMemoryDepth, value =>
        {
            _playedMapQueueDepth = Math.Max(value, 0);
            TrimPlayedMapQueue();
        }, true);
    }

    private void OnMainStationMapLoaded(GameMapPrototype proto)
    {
        EnqueuePlayedMap(proto);
    }

    private void EnqueuePlayedMap(ProtoId<GameMapPrototype> proto)
    {
        if (!_playedMapMemoryEnabled)
            return;

        _playedMapQueue.Enqueue(proto);
        TrimPlayedMapQueue();
    }

    private void TrimPlayedMapQueue()
    {
        while (_playedMapQueue.Count > _playedMapQueueDepth)
            _playedMapQueue.Dequeue();
    }
}
