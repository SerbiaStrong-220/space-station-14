using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Content.Shared.SS220.AntiCheat.FOV;
using Content.Shared.SS220.CultYogg.MiGo;

namespace Content.Server.SS220.AntiCheat.FOV;

public sealed class AntiCheatFovDetectionSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IAdminManager _adminManager = default!;
    //[Dependency] private readonly IServerNetManager _serverNet = default!;
    // if you need to disconnect cheater

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<FovEvent>(OnFovDetectionEvent);
    }

    private void OnFovDetectionEvent(FovEvent ev, EntitySessionEventArgs args)
    {
        var player = args.SenderSession.AttachedEntity;
        if (player == null)
            return;

        if (HasComp<GhostComponent>(player))
            return;

        if (_adminManager.IsAdmin(player.Value))
            return;

        if (!TryComp<EyeComponent>(player, out var eye))
            return;

        if (TryComp<MiGoComponent>(player, out var miGo) && !miGo.IsPhysicalForm)
            return;

        var serverDrawFov = eye.DrawFov;
        var serverDrawLight = eye.DrawLight;

        var mismatchFov = ev.FovFromEye != serverDrawFov || ev.FovFromComp != serverDrawFov;
        var mismatchLight = ev.LightFromEye != serverDrawLight || ev.LightFromComp != serverDrawLight;

        if (!mismatchFov && !mismatchLight)
            return;

        _adminLog.Add(
            LogType.Action,
            LogImpact.Extreme,
            $"User {ToPrettyString(player):player} triggered {nameof(AntiCheatFovDetectionSystem)})");

        //_serverNet.DisconnectChannel(args.SenderSession.Channel, "Visual cheat detected");
        // if you need to disconnect cheater
    }

}
