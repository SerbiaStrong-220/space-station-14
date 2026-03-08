// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.SS220.CultYogg.Cultists;
using Content.Shared.SS220.CultYogg.MiGo;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.CultYogg.MiGoTeleport;

public sealed class MiGoTeleportSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _userInterfaceSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MiGoTeleportComponent, MapInitEvent>(OnInit);
        SubscribeLocalEvent<MiGoTeleportComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<MiGoTeleportComponent, MiGoTeleportActionEvent>(MiGoTeleportAction);

        SubscribeLocalEvent<MiGoTeleportComponent, BoundUIOpenedEvent>(OnBoundUIOpened);
        SubscribeLocalEvent<MiGoTeleportComponent, MiGoTeleportToTargetMessage>(OnCultistsTeleportToTarget);
    }

    private void OnInit(Entity<MiGoTeleportComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.TeleportActionEntity, ent.Comp.TeleportAction);
    }

    private void OnShutdown(Entity<MiGoTeleportComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.TeleportActionEntity);
    }

    private void MiGoTeleportAction(Entity<MiGoTeleportComponent> ent, ref MiGoTeleportActionEvent args)
    {
        if (args.Handled || !TryComp<ActorComponent>(ent, out var actor))
            return;

        if (_userInterfaceSystem.TryToggleUi(ent.Owner, MiGoTeleportUiKey.Teleport, actor.PlayerSession))
            args.Handled = true;
    }

    private void OnBoundUIOpened(Entity<MiGoTeleportComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (_net.IsClient)
            return;

        if (!MiGoTeleportUiKey.Teleport.Equals(args.UiKey))
            return;

        _userInterfaceSystem.SetUiState(args.Entity, args.UiKey, new MiGoTeleportBuiState()
        {
            Warps = GetTeleportsPoints(ent),
        });
    }

    private void OnCultistsTeleportToTarget(Entity<MiGoTeleportComponent> ent, ref MiGoTeleportToTargetMessage args)
    {
        if (_net.IsClient)
            return;

        if (!TryGetEntity(args.Target, out var target))
            return;

        if (!HasComp<CultYoggComponent>(target) && !HasComp<MiGoComponent>(target))
        {
            _popup.PopupClient(Loc.GetString("cult-yogg-teleport-must-be-cultist"), ent.Owner);
            return;
        }

        if (ent.Comp.NextTeleportAvaliable != null && ent.Comp.NextTeleportAvaliable > _gameTiming.CurTime)
        {
            var timeLeft = (ent.Comp.NextTeleportAvaliable.Value - _gameTiming.CurTime).TotalSeconds;
            _popup.PopupClient(Loc.GetString("cult-yogg-teleport-cooldown", ("time", timeLeft.ToString("0.0"))), ent.Owner);
            return;
        }

        var migoMapCoord = _transformSystem.ToMapCoordinates(Transform(ent).Coordinates);

        var targetMapCoord = _transformSystem.ToMapCoordinates(Transform(target.Value).Coordinates);

        if (migoMapCoord.MapId != targetMapCoord.MapId)
        {
            _popup.PopupClient(Loc.GetString("cult-yogg-teleport-out-of-range"), ent.Owner);
            return;
        }

        WarpTo(ent, target.Value);

        ent.Comp.NextTeleportAvaliable = _gameTiming.CurTime + ent.Comp.TeleportCooldown;
    }

    private void WarpTo(EntityUid ent, EntityUid target)
    {
        _adminLogger.Add(LogType.Teleport, $"MiGo {ToPrettyString(ent):user} teleported to {ToPrettyString(target):target}");

        var xform = Transform(ent);
        _transformSystem.SetCoordinates(ent, xform, Transform(target).Coordinates);
    }

    #region Warp_list
    private List<(string, NetEntity)> GetTeleportsPoints(EntityUid owner)
    {
        List<(string, NetEntity)> warps = [];

        AddTeleportPoints<CultYoggComponent>(owner, warps);
        AddTeleportPoints<MiGoComponent>(owner, warps);

        return warps;
    }

    private void AddTeleportPoints<T>(EntityUid owner, List<(string, NetEntity)> warps) where T : IComponent
    {
        var query = EntityQueryEnumerator<T>();
        while (query.MoveNext(out var uid, out _))
        {
            if (owner == uid)
                continue;

            warps.Add((MetaData(uid).EntityName, GetNetEntity(uid)));
        }
    }

    public void UpdateTeleportTargets(EntityUid ent)
    {
        _userInterfaceSystem.SetUiState(ent, MiGoTeleportUiKey.Teleport, new MiGoTeleportBuiState()
        {
            Warps = GetTeleportsPoints(ent),
        });
    }
    #endregion
}
