using Content.Shared.SS220.Administration.Events;

namespace Content.Client.SS220.Administration.Systems;

public sealed class CustomObjectivesSystem : EntitySystem
{
    private List<CustomObjectivesPlayerInfo> _playerInfos = new();

    public IReadOnlyList<CustomObjectivesPlayerInfo> PlayerInfos => _playerInfos;

    public event Action<IReadOnlyList<CustomObjectivesPlayerInfo>>? PlayerInfosChanged;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<CustomObjectivesPlayersEvent>(OnCustomObjectivesPlayers);
    }

    private void OnCustomObjectivesPlayers(CustomObjectivesPlayersEvent ev)
    {
        _playerInfos = ev.Players;
        PlayerInfosChanged?.Invoke(_playerInfos);
    }
}
