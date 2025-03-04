// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Language;
using Content.Shared.SS220.Language.Systems;
using Robust.Shared.Network;

namespace Content.Client.SS220.Languages;

public sealed partial class LanguageSystem : SharedLanguageSystem
{
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        _net.RegisterNetMessage<ClientSelectlanguageMessage>();
    }
}

