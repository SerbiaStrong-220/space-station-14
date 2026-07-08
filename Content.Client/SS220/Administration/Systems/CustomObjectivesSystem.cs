using Content.Shared.SS220.Administration.Events;

namespace Content.Client.SS220.Administration.Systems;

public sealed partial class CustomObjectivesSystem : EntitySystem
{
    public event Action<IReadOnlyList<CustomObjectivesPlayerInfo>>? CustomObjectivesListChanged;

    private List<CustomObjectivesPlayerInfo> _players = new();

    public IReadOnlyList<CustomObjectivesPlayerInfo> Players => _players;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<CustomObjectivesPlayersEvent>(OnCustomObjectivesPlayersEvent);
    }

    private void OnCustomObjectivesPlayersEvent(CustomObjectivesPlayersEvent ev)
    {
        _players = ev.Players;
        CustomObjectivesListChanged?.Invoke(_players);
    }
}
