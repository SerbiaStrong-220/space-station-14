// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Numerics;
using Content.Server.Shuttles.Components;
using Content.Shared.SS220.CruiseControl;

namespace Content.Server.SS220.CruiseControl;

public sealed class CruiseControlSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShuttleConsoleComponent, CruiseControlMessage>(OnCruiseControlMessage);
    }

    public void DisableCruiseControlFromConsole(Entity<ShuttleConsoleComponent> console)
    {
        if (!console.Comp.CruiseControlTarget.HasValue)
            return;

        RemComp<ShuttleCruiseControlComponent>(console.Comp.CruiseControlTarget.Value);
        console.Comp.CruiseControlTarget = null;
    }

    private void OnCruiseControlMessage(Entity<ShuttleConsoleComponent> console, ref CruiseControlMessage args)
    {
        var shuttleUid = Transform(console).GridUid;
        if (!shuttleUid.HasValue)
            return;

        if (args.Enabled)
        {
            var cruiseComp = EnsureComp<ShuttleCruiseControlComponent>(shuttleUid.Value);
            console.Comp.CruiseControlTarget = shuttleUid;
            cruiseComp.LinearInput = ShuttleCruiseControlComponent.CruiseAxis * Math.Clamp(args.Throttle, -1, 1);
            Dirty(shuttleUid.Value, cruiseComp);
        }
        else
        {
            DisableCruiseControlFromConsole(console);
        }
    }
}
