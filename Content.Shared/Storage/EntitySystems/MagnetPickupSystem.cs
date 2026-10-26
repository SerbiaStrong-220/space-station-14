using Content.Shared.Inventory;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Storage.Components;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Shared.Storage.EntitySystems;

/// <summary>
/// <see cref="MagnetPickupComponent"/>
/// </summary>
public sealed class MagnetPickupSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    [Dependency] private ItemToggleSystem _itemToggle = default!; // SS220-MagnetToggle
    [Dependency] private readonly EntityQuery<PhysicsComponent> _physicsQuery = default!;

    private static readonly TimeSpan ScanDelay = TimeSpan.FromSeconds(1);


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MagnetPickupComponent, MapInitEvent>(OnMagnetMapInit);
        // SS220-MagnetToggle begin
        SubscribeLocalEvent<MagnetPickupComponent, ItemToggledEvent>(OnMagnetToggled);
        SubscribeLocalEvent<MagnetPickupComponent, GetVerbsEvent<ActivationVerb>>(OnMagnetToggleVerb);
        // SS220-MagnetToggle end
    }

    private void OnMagnetMapInit(EntityUid uid, MagnetPickupComponent component, MapInitEvent args)
    {
        component.NextScan = _timing.CurTime;
    }

    // SS220-MagnetToggle begin
    private void OnMagnetToggled(Entity<MagnetPickupComponent> ent, ref ItemToggledEvent args)
    {
        ent.Comp.Enabled = args.Activated;
        Dirty(ent, ent.Comp);
    }

    private void OnMagnetToggleVerb(Entity<MagnetPickupComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !TryComp(ent, out ItemToggleComponent? toggle))
            return;

        var user = args.User;
        args.Verbs.Add(new ActivationVerb
        {
            Text = !toggle.Activated ? Loc.GetString(toggle.VerbToggleOn) : Loc.GetString(toggle.VerbToggleOff),
            Act = () => _itemToggle.Toggle((ent.Owner, toggle), user, predicted: toggle.Predictable),
        });
    }
    // SS220-MagnetToggle end

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<MagnetPickupComponent, StorageComponent, TransformComponent, MetaDataComponent>();
        var currentTime = _timing.CurTime;

        while (query.MoveNext(out var uid, out var comp, out var storage, out var xform, out var meta))
        {
            if (comp.NextScan > currentTime)
                continue;

            // SS220-MagnetToggle begin
            if (!comp.Enabled)
                continue;
            // SS220-MagnetToggle end

            comp.NextScan += ScanDelay;
            Dirty(uid, comp);

            // SS220-OreBagBorgMagnet begin
            if (!comp.WorksOutsideSlot)
            {

             if (!_inventory.TryGetContainingSlot((uid, xform, meta), out var slotDef))
                continue;
            
             if ((slotDef.SlotFlags & comp.SlotFlags) == 0x0)
                continue;
            }
            // SS220-OreBagBorgMagnet end

            // No space
            if (!_storage.HasSpace((uid, storage)))
                continue;

            var parentUid = xform.ParentUid;
            var playedSound = false;
            var finalCoords = xform.Coordinates;
            var moverCoords = _transform.GetMoverCoordinates(uid, xform);

            foreach (var near in _lookup.GetEntitiesInRange(uid, comp.Range, LookupFlags.Dynamic | LookupFlags.Sundries))
            {
                if (_whitelistSystem.IsWhitelistFail(storage.Whitelist, near))
                    continue;

                if (!_physicsQuery.TryGetComponent(near, out var physics) || physics.BodyStatus != BodyStatus.OnGround)
                    continue;

                if (near == parentUid)
                    continue;

                // TODO: Probably move this to storage somewhere when it gets cleaned up
                // TODO: This sucks but you need to fix a lot of stuff to make it better
                // the problem is that stack pickups delete the original entity, which is fine, but due to
                // game state handling we can't show a lerp animation for it.
                var nearXform = Transform(near);
                var nearMap = _transform.GetMapCoordinates(near, xform: nearXform);
                var nearCoords = _transform.ToCoordinates(moverCoords.EntityId, nearMap);

                if (!_storage.Insert(uid, near, out var stacked, storageComp: storage, playSound: !playedSound))
                    continue;

                // Play pickup animation for either the stack entity or the original entity.
                if (stacked != null)
                    _storage.PlayPickupAnimation(stacked.Value, nearCoords, finalCoords, nearXform.LocalRotation);
                else
                    _storage.PlayPickupAnimation(near, nearCoords, finalCoords, nearXform.LocalRotation);

                playedSound = true;
            }
        }
    }
}
