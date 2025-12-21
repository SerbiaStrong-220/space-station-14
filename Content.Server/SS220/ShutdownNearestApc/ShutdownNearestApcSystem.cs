using Content.Server.Administration.Logs;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Database;
using Content.Shared.SS220.ShutdownNearestApc;

namespace Content.Server.SS220.ShutdownNearestApc;

/// <summary>
/// System that handles the ShutdownNearestApcEvent triggered by store purchases.
/// Finds APCs within a given radius and disables their main breakers.
/// </summary>
public sealed class ShutdownNearestApcSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ApcSystem _apc = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ShutdownNearestApcEvent>(OnShutdownNearestApc);
    }

    /// <summary>
    /// Handles the ShutdownNearestApcEvent by locating nearby APCs and disabling their breakers.
    /// Logs the action for administrative tracking.
    /// </summary>
    /// <param name="args">Event data containing purchaser, radius, and store context.</param>
    private void OnShutdownNearestApc(ShutdownNearestApcEvent args)
    {
        var query = _lookup.GetEntitiesInRange<ApcComponent>(Transform(args.Purchaser).Coordinates, args.Radius);
        var count = 0;

        foreach (var apc in query)
        {
            if (!apc.Comp.MainBreakerEnabled)
                continue;

            _apc.ApcToggleBreaker(apc.Owner, apc);
            count++;
        }

        var name = string.Empty;

        if (args.Listing.Name != null)
            name = Loc.GetString(args.Listing.Name);

        _adminLog.Add(LogType.Action, LogImpact.Medium,
            $"{ToPrettyString(args.Purchaser):player} shut down {count} APC(s) within {args.Radius} meters, using {name}");
    }
}
