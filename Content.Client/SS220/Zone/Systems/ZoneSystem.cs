// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.SS220.Zone.UI;
using Content.Shared.SS220.Zone.Systems;
using Content.Shared.SS220.Zone;
using Content.Shared.SS220.Zone.Components;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.Zone.Systems;

public sealed partial class ZoneSystem : SharedZoneSystem
{
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    private ZonesControlUIController _controller = default!;

    protected override IRelationsUpdateData RelationUpdateData => _relationUpdateData;

    private readonly ClientRelationUpdateData _relationUpdateData = new();

    public override void Initialize()
    {
        base.Initialize();

        _controller = _ui.GetUIController<ZonesControlUIController>();

        SubscribeLocalEvent<ZoneComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ZoneComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);

        SubscribeNetworkEvent<HandleRelationUpdateDataStateMessage>(HandleRelationUpdateDataState);
    }

    private void OnInit(Entity<ZoneComponent> ent, ref ComponentInit args)
    {
        _controller.RefreshWindow();
    }

    private void OnAfterAutoHandleState(Entity<ZoneComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateZoneCache(ent);

        _controller.RefreshWindow();
    }

    protected override void OnZoneShutdown(Entity<ZoneComponent> entity, ref ComponentShutdown args)
    {
        base.OnZoneShutdown(entity, ref args);
        _controller.RefreshWindow();
    }

    private void HandleRelationUpdateDataState(HandleRelationUpdateDataStateMessage args)
    {
        _relationUpdateData.Clear();

        foreach (var netUid in args.State.Entities)
            _relationUpdateData.Entities.Add(GetEntity(netUid));

        foreach (var compName in args.State.Components)
        {
            if (!Factory.TryGetRegistration(compName, out var reg))
                continue;

            _relationUpdateData.Components.Add(reg);
        }
    }

    public void CreateZoneRequest(
        NetEntity parent,
        EntProtoId<ZoneComponent> protoId,
        List<Box2> area,
        string? name = null,
        Color? color = null)
    {
        var msg = new CreateZoneRequestMessage(parent, protoId, area, name, color);
        RaiseNetworkEvent(msg);
    }

    public void ChangeZoneRequest(
        NetEntity zone,
        NetEntity? parent = null,
        EntProtoId<ZoneComponent>? protoId = null,
        List<Box2>? area = null,
        string? name = null,
        Color? color = null)
    {
        var msg = new ChangeZoneRequestMessage(zone, parent, protoId, area, name, color);
        RaiseNetworkEvent(msg);
    }

    public void DeleteZoneRequest(NetEntity zone)
    {
        var msg = new DeleteZoneRequestMessage(zone);
        RaiseNetworkEvent(msg);
    }

    public override bool RegisterRelationUpdate(EntityUid uid, object registrator)
    {
        return false;
    }

    public override bool RegisterRelationUpdate(Type componentType, object registrator)
    {
        return false;
    }

    public override bool UnregisterRelationUpdate(EntityUid uid, object registrator)
    {
        return false;
    }

    public override bool UnregisterRelationUpdate(Type componentType, object registrator)
    {
        return false;
    }

    public override bool UnregisterRelationUpdateForced(EntityUid uid)
    {
        return false;
    }

    public override bool UnregisterRelationUpdateForced(Type componentType)
    {
        return false;
    }

    #region Relation update API
    private sealed class ClientRelationUpdateData() : IRelationsUpdateData
    {
        public HashSet<EntityUid> Entities { get; set; } = [];

        public HashSet<ComponentRegistration> Components { get; set; } = [];

        public void Clear()
        {
            Entities.Clear();
            Components.Clear();
        }
    }
    #endregion
}
