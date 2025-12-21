// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Humanoid.Prototypes;
using Content.Shared.SS220.Players;
using Robust.Client;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.Species;

public sealed partial class SpeciesRequirementsManager
{
    [Dependency] private readonly IBaseClient _client = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly INetManager _net = default!;

    private readonly List<string> _speciesBans = [];

    private ISawmill _sawmill = default!;

    public event Action? Updated;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("species_requirements");

        _net.RegisterNetMessage<MsgSpeciesBans>(ReceiveSpeciesBans);

        _client.RunLevelChanged += ClientOnRunLevelChanged;
    }

    private void ClientOnRunLevelChanged(object? sender, RunLevelChangedEventArgs e)
    {
        if (e.NewLevel is ClientRunLevel.Initialize)
            _speciesBans.Clear();
    }

    private void ReceiveSpeciesBans(MsgSpeciesBans msg)
    {
        _sawmill.Debug($"Received speciesban info containing {msg.Bans.Count} entries.");

        _speciesBans.Clear();
        _speciesBans.AddRange(msg.Bans);
        Updated?.Invoke();
    }

    public bool IsBanned(string speciesId)
    {
        if (!_proto.TryIndex<SpeciesPrototype>(speciesId, out var species))
            return false;

        return IsBanned(species);
    }

    public bool IsBanned(SpeciesPrototype species)
    {
        return _speciesBans.Contains(species.ID);
    }
}
