using Content.Server.GameTicking;
using Content.Server.Shuttles.Systems;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Corvax.CCCVars;
using Content.Shared.Shuttles.Components;
using Content.Shared.SS220.RoundEnd;
using Robust.Server.Player;
using Robust.Shared.Configuration;

namespace Content.Server.Corvax.PeacefulRoundEnd;

public sealed class PeacefulRoundEndSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly EmergencyShuttleSystem _emergency = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    
    private bool _isEnabled = false;
    
    public override void Initialize()
    {
        base.Initialize();
        _cfg.OnValueChanged(CCCVars.PeacefulRoundEnd, v => _isEnabled = v, true);
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEnded);
    }

    private void OnRoundEnded(RoundEndTextAppendEvent ev)
    {
        if (!_isEnabled) return;
// SS220-centcomm-grief-start
        var maps = _emergency.GetCentcommMaps();

        var query = AllEntityQuery<FTLMapComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            maps.Add(uid);
        }

        foreach (var session in _playerManager.Sessions)
        {
            if (!session.AttachedEntity.HasValue) continue;

            var xform = Transform(session.AttachedEntity.Value);
            if (xform.MapUid is not null && maps.Contains(xform.MapUid.Value))
            {
                EnsureComp<PacifiedComponent>(session.AttachedEntity.Value);
                EnsureComp<RoundEndPacifiedComponent>(session.AttachedEntity.Value);
            }
        }
// SS220-centcomm-grief-end
    }
}
