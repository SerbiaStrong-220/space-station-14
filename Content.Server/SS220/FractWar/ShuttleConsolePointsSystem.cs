using Content.Server.SS220.FractWar;
using Content.Server.Chat.Systems;
using Content.Shared.Destructible;
using Content.Shared.SS220.FractWar;
using Robust.Server.GameObjects;
using Robust.Shared.Maths;

namespace Content.Server.SS220.FractWar;

public sealed class ShuttleConsolePointsSystem : EntitySystem
{
    [Dependency] private readonly FractWarRuleSystem _fractWarRule = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;

    private static readonly Color NtAnnouncementColor = Color.FromHex("#0c82c7");
    private static readonly Color SyndAnnouncementColor = Color.FromHex("#8f4a4b");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShuttleConsolePointsComponent, DestructionEventArgs>(OnConsoleDestroyed);
    }

    private void OnConsoleDestroyed(Entity<ShuttleConsolePointsComponent> entity, ref DestructionEventArgs args)
    {
        AnnounceConsoleDestroyed(entity);

        var gameRule = _fractWarRule.GetActiveGameRule();
        if (gameRule is null)
            return;

        var comp = entity.Comp;
        if (string.IsNullOrEmpty(comp.Fraction))
            return;

        if (!gameRule.FractionsWinPoints.TryAdd(comp.Fraction, comp.PointsOnDestroy))
            gameRule.FractionsWinPoints[comp.Fraction] += comp.PointsOnDestroy;
    }

    private void AnnounceConsoleDestroyed(Entity<ShuttleConsolePointsComponent> entity)
    {
        var prototype = MetaData(entity).EntityPrototype?.ID;
        if (prototype is not ("FractWarShuttleConsoleSyndicate" or "FractWarShuttleConsoleNT"))
            return;

        var transform = Transform(entity);
        var gridName = transform.GridUid is { } gridUid
            ? MetaData(gridUid).EntityName
            : MetaData(entity).EntityName;

        var (fractionName, color) = prototype switch
        {
            "FractWarShuttleConsoleNT" => (Loc.GetString("flag-fraction-NT"), NtAnnouncementColor),
            "FractWarShuttleConsoleSyndicate" => (Loc.GetString("flag-fraction-Synd"), SyndAnnouncementColor),
            _ => (string.Empty, Color.White),
        };

        _chatSystem.DispatchGlobalAnnouncement(
            Loc.GetString("fractwar-console-destroyed-announcement"),
            sender: Loc.GetString("fractwar-console-destroyed-sender", ("grid", gridName), ("fraction", fractionName)),
            playSound: false,
            colorOverride: color,
            playTTS: false,
            playPrerecordedSound: false);
    }
}
