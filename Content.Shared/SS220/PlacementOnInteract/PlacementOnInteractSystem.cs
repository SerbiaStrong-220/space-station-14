using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.PlacementOnInteract;

public sealed partial class PlacementOnInteractSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlacementOnInteractComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
        SubscribeLocalEvent<PlacementOnInteractComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteractUsing(Entity<PlacementOnInteractComponent> entity, ref AfterInteractUsingEvent args)
    {
        entity.Comp.IsActive = !entity.Comp.IsActive;
    }

    private void OnAfterInteract(Entity<PlacementOnInteractComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || !entity.Comp.IsActive)
            return;

        var (uid, comp) = entity;
        var user = args.User;
        var location = args.ClickLocation;

        if (!location.IsValid(EntityManager))
            return;

        Direction direction = 0;

        var doAfterTime = TimeSpan.FromSeconds(comp.DoAfter);
        var ev = new PlacementOnInteractDoAfterEvent(GetNetCoordinates(location), direction, comp.ProtoId);

        var doAfterArgs = new DoAfterArgs(EntityManager, user, doAfterTime, ev, uid, args.Target, uid)
        {
            BreakOnDamage = true,
            BreakOnHandChange = true,
            BreakOnMove = true,
            AttemptFrequency = AttemptFrequency.EveryTick,
            CancelDuplicate = false,
            BlockDuplicate = false
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }
}

[Serializable, NetSerializable]
public sealed partial class PlacementOnInteractDoAfterEvent : DoAfterEvent
{
    public NetCoordinates Coordinates;

    public Direction Direction;

    public EntProtoId ProtoId;

    public PlacementOnInteractDoAfterEvent(NetCoordinates coordinates, Direction direction, EntProtoId protoId)
    {
        Coordinates = coordinates;
        Direction = direction;
        ProtoId = protoId;
    }

    public override DoAfterEvent Clone() => this;
}
