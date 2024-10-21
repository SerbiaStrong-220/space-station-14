// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.EUI;
using Content.Server.Popups;
using Content.Shared.Damage;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Robust.Server.Player;
using Content.Shared.SS220.CultYogg.Components;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Network;
using Content.Server.SS220.GameTicking.Rules;

namespace Content.Server.SS220.CultYogg;

public sealed partial class SacraficialReplacementSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private Dictionary<(EntityUid, NetUserId), TimeSpan> _sacraficialsLeftBody = new();

    //Count down the moment when sacraficial will be raplaced
    private TimeSpan _beforeReplacementCooldown = TimeSpan.FromSeconds(300);//ToDo set timer

    //Count down the moment when cultists will get an anounce about replacement
    private TimeSpan _announceReplacementCooldown = TimeSpan.FromSeconds(100);//ToDo set timer

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultYoggSacrificialComponent, PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<CultYoggSacrificialComponent, PlayerDetachedEvent>(OnPlayerDetached);
    }
    private void OnPlayerAttached(Entity<CultYoggSacrificialComponent> ent, ref PlayerAttachedEvent args)
    {
        _sacraficialsLeftBody.Remove((ent, args.Player.UserId));

        ent.Comp.ReplacementAnnounceWereSend = false;

        var meta = MetaData(ent);

        var ev = new CultYoggAnouncementEvent(ent, Loc.GetString("cult-yogg-sacraficial-cant-be-replaced", ("name", meta.EntityName)));
        RaiseLocalEvent(ent, ref ev, true);
    }

    private void OnPlayerDetached(Entity<CultYoggSacrificialComponent> ent, ref PlayerDetachedEvent args)
    {
        _sacraficialsLeftBody.Add((ent, args.Player.UserId), _timing.CurTime);
    }

    private void ReplacamantStatusAnnounce(EntityUid uid)
    {
        if (!TryComp<CultYoggSacrificialComponent>(uid, out var comp))
            return;

        comp.ReplacementAnnounceWereSend = true;

        var meta = MetaData(uid);

        var ev = new CultYoggAnouncementEvent(uid, Loc.GetString("cult-yogg-sacraficial-may-be-replaced", ("name", meta.EntityName)));
        RaiseLocalEvent(uid, ref ev, true);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        foreach (var pair in _sacraficialsLeftBody)
        {
            if (_timing.CurTime < pair.Value + _announceReplacementCooldown)
                ReplacamantStatusAnnounce(pair.Key.Item1);

            if (_timing.CurTime < pair.Value + _beforeReplacementCooldown)
                continue;

            var ev = new SacraficialReplacementEvent(pair.Key.Item1, pair.Key.Item2);
            RaiseLocalEvent(pair.Key.Item1, ref ev, true);

            _sacraficialsLeftBody.Remove(pair.Key);
        }
    }
}

[ByRefEvent, Serializable]
public sealed class SacraficialReplacementEvent : EntityEventArgs
{
    public readonly EntityUid Entity;
    public readonly NetUserId Player;

    public SacraficialReplacementEvent(EntityUid entity, NetUserId player)
    {
        Entity = entity;
        Player = player;
    }
}
