// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.EUI;
using Content.Server.Ghost;
using Content.Shared.Mind;
using Content.Shared.SS220.CultYogg.MiGo;
using Robust.Shared.Player;

namespace Content.Server.SS220.CultYogg.MiGo;

public sealed class CultYoggHealSystem : SharedCultYoggHealSystem
{

    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void SendReturnToBodyEui(EntityUid ent)
    {
        if (!_mind.TryGetMind(ent, out _, out var mind))
            return;

        if (!_player.TryGetSessionById(mind.UserId, out var playerSession))
            return;

        if (mind.CurrentEntity == ent)
            return;

        _euiManager.OpenEui(new ReturnToBodyEui(mind, _mind, _player), playerSession);
    }
}
