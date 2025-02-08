// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared.SS220.GateGunDungeon;

/// <summary>
/// This handles long-range weapons, prohibits shooting if the player is at the station.
/// </summary>
public sealed class GateGunDungeonSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<GateGunDungeonComponent, ShotAttemptedEvent>(OnShoot);
    }

    private void OnShoot(Entity<GateGunDungeonComponent> ent, ref ShotAttemptedEvent args)
    {
       var gridUid = _transform.GetGrid(ent.Owner);

       if (gridUid == null)
           args.Cancel();

       if (!HasComp<GateDungeonMapComponent>(gridUid))
           args.Cancel();
    }
}

