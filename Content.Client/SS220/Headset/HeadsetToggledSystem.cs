using Content.Shared.SS220.Headset;

namespace Content.Client.SS220.Headset;

public sealed class HeadsetToggledSystem : SharedHeadsetToggledSystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeNetworkEvent<HeadsetSetListEvent>(OnSetList);
    }

    private void OnSetList(HeadsetSetListEvent args)
    {
        var headSet = GetEntity(args.Owner);

        var state = new HeadsetBoundInterfaceState(args.ChannelList);
        _ui.SetUiState(headSet, HeadsetKey.Key, state);
    }
}
