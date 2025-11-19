using Content.Shared.FixedPoint;
using Content.Shared.Radio.Components;
using Content.Shared.SS220.Headset;
using Content.Shared.SS220.Radio.Components;

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

        RadioFrequencySettings? frequencySetting = null;
        if (TryComp<EncryptionKeyHolderComponent>(headSet, out var keyHolder))
        {
            foreach (var key in keyHolder.KeyContainer.ContainedEntities)
            {
                if (!TryComp<RadioEncryptionKeyComponent>(key, out var radioEncryption))
                    continue;

                frequencySetting = new(radioEncryption.LowerFrequencyBorder, radioEncryption.UpperFrequencyBorder, radioEncryption.RadioFrequency);
                break;
            }
        }

        var state = new HeadsetBoundInterfaceState(args.ChannelList, frequencySetting);
        _ui.SetUiState(headSet, HeadsetKey.Key, state);
    }
}
