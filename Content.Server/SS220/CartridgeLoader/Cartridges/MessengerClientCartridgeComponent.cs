// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Server.SS220.CartridgeLoader.Cartridges;

[RegisterComponent]
public sealed partial class MessengerClientCartridgeComponent : Component
{
    /// <summary>
    /// Loader device (PDA) the cartridge is installed in.
    /// </summary>
    public EntityUid Loader;

    /// <summary>
    /// The last id card with its own account that was in the pda of this cartridge. It stays after the card
    /// is taken out, so the agent id card can take its accounts over.
    /// </summary>
    public EntityUid? LastIdCard;

    /// <summary>
    /// Network addresses of the servers that answered this client.
    /// </summary>
    public Dictionary<EntityUid, string> ServerAddresses = new();

    /// <summary>
    /// Server the client talks to.
    /// </summary>
    public EntityUid? ActiveServer;

    /// <summary>
    /// When true, the system queues the full messenger state from the active server.
    /// </summary>
    public bool SendState;

    /// <summary>
    /// Ui states waiting to be sent. A loader keeps only the last state set during a tick,
    /// so the system sends one state per update.
    /// </summary>
    public Queue<BoundUserInterfaceState> PendingStates = new();
}
