// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.GameTicking;
using Content.Shared.Examine;
using Content.Shared.SS220.Clocks;

namespace Content.Server.SS220.Clocks;

/// <summary>
/// This system makes clocks state time when examined.
/// </summary>
public sealed class PhysicalClockSystem : EntitySystem
{
    [Dependency] private readonly IEntitySystemManager _entitySystem = default!;

    // <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PhysicalClockComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<PhysicalClockComponent> ent, ref ExaminedEvent args)
    {
        if (!ent.Comp.Enabled)
            return;

        var gameTicker = _entitySystem.GetEntitySystem<GameTicker>();
        var stationTime = gameTicker.RoundDuration();

        args.PushMarkup(Loc.GetString("comp-clocks-time-description",
            ("time", stationTime.ToString("hh\\:mm\\:ss"))));
    }
}
