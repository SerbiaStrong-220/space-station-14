// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Administration.Logs;
using Content.Server.CartridgeLoader;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Power.Components;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.GameTicking;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.SS220.Messenger;

/// <summary>
/// Server of the messenger. The logic is split by the topic:
/// registration of the clients, contacts, chats and messages, network packets, the station AI.
/// </summary>
public sealed partial class MessengerServerSystem : EntitySystem
{
    [Dependency] private CartridgeLoaderSystem _cartridgeLoader = default!;
    [Dependency] private DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private SharedIdCardSystem _idCard = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private AccessReaderSystem _accessSystem = default!;
    [Dependency] private SharedContainerSystem _containerSystem = default!;
    [Dependency] private SharedGameTicker _gameTicker = default!;
    [Dependency] private IAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitializeNetwork();
        InitializeRegistration();
        InitializeContacts();
        InitializeAi();
        InitializeAiContact();
        InitializeAgent();
    }

    private bool IsPowered(EntityUid uid)
    {
        return TryComp<ApcPowerReceiverComponent>(uid, out var power) && power.Powered;
    }

    private bool HasServerAccess(Entity<MessengerServerComponent> ent, EntityUid idCardUid)
    {
        return TryComp<AccessReaderComponent>(ent, out var reader) && _accessSystem.IsAllowed(idCardUid, ent, reader);
    }

    public List<uint> ActiveServersFrequency(MapId mapId)
    {
        var activeServersFrequency = new List<uint>();
        var query = EntityQueryEnumerator<DeviceNetworkComponent,
            MessengerServerComponent,
            ApcPowerReceiverComponent,
            TransformComponent>();

        while (query.MoveNext(out _, out var deviceNet, out _, out var power, out var transform))
        {
            if (transform.MapID != mapId || !power.Powered)
                continue;

            if (deviceNet.ReceiveFrequency != null)
                activeServersFrequency.Add(deviceNet.ReceiveFrequency.Value);
        }

        return activeServersFrequency;
    }
}
