using Content.Server.Chat.Systems;
using Content.Shared.Destructible;
using Content.Shared.SS220.FractWar;
using Robust.Shared.Prototypes;
using Color = Robust.Shared.Maths.Color;

namespace Content.Server.SS220.FractWar;

public sealed partial class ShuttleConsolePointsSystem : EntitySystem
{
    [Dependency] private FractWarRuleSystem _fractWarRule = default!;
    [Dependency] private ChatSystem _chatSystem = default!;

    private static readonly EntProtoId NtConsolePrototype = "FractWarShuttleConsoleNT";
    private static readonly EntProtoId SyndicateConsolePrototype = "FractWarShuttleConsoleSyndicate";
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
        if (prototype is null)
            return;

        if (prototype != SyndicateConsolePrototype && prototype != NtConsolePrototype)
            return;

        var transform = Transform(entity);
        var gridName = transform.GridUid is { } gridUid
            ? MetaData(gridUid).EntityName
            : MetaData(entity).EntityName;

        var fractionName = prototype == NtConsolePrototype
            ? Loc.GetString("flag-fraction-NT")
            : Loc.GetString("flag-fraction-Synd");

        var color = prototype == NtConsolePrototype
            ? NtAnnouncementColor
            : SyndAnnouncementColor;

        _chatSystem.DispatchGlobalAnnouncement(
            Loc.GetString("fractwar-console-destroyed-announcement"),
            sender: Loc.GetString("fractwar-console-destroyed-sender", ("grid", gridName), ("fraction", fractionName)),
            playSound: false,
            colorOverride: color,
            playTTS: false,
            playPrerecordedSound: false);
    }
}
