using Content.Shared.SS220.GhostHearing;

namespace Content.Client.SS220.GhostHearing;

public sealed class GhostHearingSystem : SharedGhostHearingSystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<GhostHearingSetListEvent>(OnSetList);
    }

    private void OnSetList(GhostHearingSetListEvent args)
    {
        var owner = GetEntity(args.Owner);

        var state = new GhostHearingBoundUIState(args.ChannelList);
        _ui.SetUiState(owner, GhostHearingKey.Key, state);
    }
}
