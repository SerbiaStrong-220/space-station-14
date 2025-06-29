using Content.Shared.Power.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Shared.Materials.OreSilo;

public abstract class SharedOreSiloSystem : EntitySystem
{
    [Dependency] private readonly SharedMaterialStorageSystem _materialStorage = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _powerReceiver = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<OreSiloClientComponent> _clientQuery;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<OreSiloComponent, MapInitEvent>(OnMapInit); // SS220 Add silo linking in mapping
        SubscribeLocalEvent<OreSiloComponent, ToggleOreSiloClientMessage>(OnToggleOreSiloClient);
        SubscribeLocalEvent<OreSiloComponent, ComponentShutdown>(OnSiloShutdown);
        Subs.BuiEvents<OreSiloComponent>(OreSiloUiKey.Key,
            subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnBoundUIOpened);
        });


        SubscribeLocalEvent<OreSiloClientComponent, GetStoredMaterialsEvent>(OnGetStoredMaterials);
        SubscribeLocalEvent<OreSiloClientComponent, ConsumeStoredMaterialsEvent>(OnConsumeStoredMaterials);
        SubscribeLocalEvent<OreSiloClientComponent, ComponentShutdown>(OnClientShutdown);

        _clientQuery = GetEntityQuery<OreSiloClientComponent>();
    }

    // SS220 Add silo linking in mapping begin
    private void OnMapInit(Entity<OreSiloComponent> entity, ref MapInitEvent args)
    {
        SynchronizeWithDeviceList(entity);
    }
    // SS220 Add silo linking in mapping end

    private void OnToggleOreSiloClient(Entity<OreSiloComponent> ent, ref ToggleOreSiloClientMessage args)
    {
        var client = GetEntity(args.Client);

        if (!_clientQuery.TryComp(client, out var clientComp))
            return;

        if (ent.Comp.Clients.Contains(client)) // remove client
        {
            // SS220 Add silo linking in mapping begin
            //clientComp.Silo = null;
            //Dirty(client, clientComp);
            //ent.Comp.Clients.Remove(client);
            //Dirty(ent);

            //UpdateOreSiloUi(ent);

            RemoveClient(ent, (client, clientComp));
            // SS220 Add silo linking in mapping end
        }
        else // add client
        {
            if (!CanTransmitMaterials((ent, ent), client))
                return;

            // SS220 Add silo linking in mapping begin
            //var clientMats = _materialStorage.GetStoredMaterials(client, true);
            //var inverseMats = new Dictionary<string, int>();
            //foreach (var (mat, amount) in clientMats)
            //{
            //    inverseMats.Add(mat, -amount);
            //}
            //_materialStorage.TryChangeMaterialAmount(client, inverseMats, localOnly: true);
            //_materialStorage.TryChangeMaterialAmount(ent.Owner, clientMats);

            //ent.Comp.Clients.Add(client);
            //Dirty(ent);
            //clientComp.Silo = ent;
            //Dirty(client, clientComp);

            //UpdateOreSiloUi(ent);

            AddClient(ent, (client, clientComp));
            // SS220 Add silo linking in mapping end
        }

        SynchronizeWithDeviceList(ent, false); // SS220 Add silo linking in mapping
    }

    private void OnBoundUIOpened(Entity<OreSiloComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateOreSiloUi(ent);
    }

    private void OnSiloShutdown(Entity<OreSiloComponent> ent, ref ComponentShutdown args)
    {
        foreach (var client in ent.Comp.Clients)
        {
            if (!_clientQuery.TryComp(client, out var comp))
                continue;

            comp.Silo = null;
            Dirty(client, comp);
        }
    }

    protected virtual void UpdateOreSiloUi(Entity<OreSiloComponent> ent)
    {

    }

    private void OnGetStoredMaterials(Entity<OreSiloClientComponent> ent, ref GetStoredMaterialsEvent args)
    {
        if (args.LocalOnly)
            return;

        if (ent.Comp.Silo is not { } silo)
            return;

        if (!CanTransmitMaterials(silo, ent))
            return;

        var materials = _materialStorage.GetStoredMaterials(silo);

        foreach (var (mat, amount) in materials)
        {
            // Don't supply materials that they don't usually have access to.
            if (!_materialStorage.IsMaterialWhitelisted((args.Entity, args.Entity), mat))
                continue;

            var existing = args.Materials.GetOrNew(mat);
            args.Materials[mat] = existing + amount;
        }
    }

    private void OnConsumeStoredMaterials(Entity<OreSiloClientComponent> ent, ref ConsumeStoredMaterialsEvent args)
    {
        if (args.LocalOnly)
            return;

        if (ent.Comp.Silo is not { } silo || !TryComp<MaterialStorageComponent>(silo, out var materialStorage))
            return;

        if (!CanTransmitMaterials(silo, ent))
            return;

        foreach (var (mat, amount) in args.Materials)
        {
            if (!_materialStorage.TryChangeMaterialAmount(silo, mat, amount, materialStorage))
                continue;
            args.Materials[mat] = 0;
        }
    }

    private void OnClientShutdown(Entity<OreSiloClientComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<OreSiloComponent>(ent.Comp.Silo, out var silo))
            return;

        silo.Clients.Remove(ent);
        Dirty(ent.Comp.Silo.Value, silo);
        UpdateOreSiloUi((ent.Comp.Silo.Value, silo));
        SynchronizeWithDeviceList((ent.Comp.Silo.Value, silo), false); // SS220 Add silo linking in mapping
    }

    /// <summary>
    /// Checks if a given client fulfills the criteria to link/receive materials from an ore silo.
    /// </summary>
    [PublicAPI]
    public bool CanTransmitMaterials(Entity<OreSiloComponent?, TransformComponent?> silo, EntityUid client)
    {
        if (!Resolve(silo, ref silo.Comp1, ref silo.Comp2))
            return false;

        if (!_powerReceiver.IsPowered(silo.Owner))
            return false;

        if (_transform.GetGrid(client) != _transform.GetGrid(silo.Owner))
            return false;

        if (!silo.Comp1.EntireGrid && // SS220 OreSilo works across the entire grid
            !_transform.InRange((silo.Owner, silo.Comp2), client, silo.Comp1.Range))
            return false;

        return true;
    }

    // SS220 Add silo linking in mapping begin
    protected virtual void SynchronizeWithDeviceList(Entity<OreSiloComponent> entity, bool? deviceNetworkPriority = null)
    {
    }

    public void AddClient(Entity<OreSiloComponent> silo, Entity<OreSiloClientComponent> client)
    {
        var clientMats = _materialStorage.GetStoredMaterials(client.Owner, true);
        var inverseMats = new Dictionary<string, int>();
        foreach (var (mat, amount) in clientMats)
        {
            inverseMats.Add(mat, -amount);
        }
        _materialStorage.TryChangeMaterialAmount(client.Owner, inverseMats, localOnly: true);
        _materialStorage.TryChangeMaterialAmount(silo.Owner, clientMats);

        silo.Comp.Clients.Add(client);
        Dirty(silo);
        client.Comp.Silo = silo;
        Dirty(client);

        UpdateOreSiloUi(silo);
    }

    public void RemoveClient(Entity<OreSiloComponent> silo, Entity<OreSiloClientComponent> client)
    {
        client.Comp.Silo = null;
        Dirty(client);
        silo.Comp.Clients.Remove(client);
        Dirty(silo);

        UpdateOreSiloUi((silo, silo.Comp));
    }

    public void UpdateClientsList(Entity<OreSiloComponent> entity, IEnumerable<EntityUid> clients, bool merge = false)
    {
        var oldClients = entity.Comp.Clients.ToList();
        var newClients = clients.ToHashSet();

        if (merge)
            newClients.UnionWith(entity.Comp.Clients);

        foreach (var client in oldClients)
        {
            if (newClients.Contains(client) ||
                !TryComp<OreSiloClientComponent>(client, out var clientComp))
                continue;

            RemoveClient(entity, (client, clientComp));
        }

        foreach (var client in newClients)
        {
            if (entity.Comp.Clients.Contains(client) ||
                !TryComp<OreSiloClientComponent>(client, out var clientComp))
                continue;

            AddClient(entity, (client, clientComp));
        }
    }
    // SS220 Add silo linking in mapping end
}
