// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chat;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;

namespace Content.Shared.SS220.Ipc;

public abstract partial class SharedIpcSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IpcComponent, GetDefaultRadioChannelEvent>(OnGetDefaultRadioChannel);
    }

    private void OnGetDefaultRadioChannel(Entity<IpcComponent> ent, ref GetDefaultRadioChannelEvent args)
    {
        if (!TryComp<EncryptionKeyHolderComponent>(ent, out var keyHolder))
            return;

        args.Channel ??= keyHolder.DefaultChannel ?? SharedChatSystem.CommonChannel;
    }
}
