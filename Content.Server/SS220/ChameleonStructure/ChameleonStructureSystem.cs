// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.IdentityManagement;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Prototypes;
using Content.Shared.SS220.ChameleonStructure;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.ChameleonStructure;

public sealed class ChameleonStructureSystem : SharedChameleonStructureSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly IdentitySystem _identity = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChameleonStructureComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ChameleonStructureComponent, ChameleonStructurePrototypeSelectedMessage>(OnSelected);
    }

    private void OnMapInit(Entity<ChameleonStructureComponent> ent, ref MapInitEvent args)
    {
        SetSelectedPrototype(ent, ent.Comp.Default, true);
    }

    private void OnSelected(Entity<ChameleonStructureComponent> ent, ref ChameleonStructurePrototypeSelectedMessage args)
    {
        SetSelectedPrototype(ent, args.SelectedId);
    }

    private void UpdateUi(Entity<ChameleonStructureComponent> ent)
    {
        var state = new ChameleonStructureBoundUserInterfaceState(ent.Comp.Default, ent.Comp.RequireTag);
        UI.SetUiState(ent.Owner, ChameleonStructureUiKey.Key, state);
    }

    /// <summary>
    ///     Change chameleon items name, description and sprite to mimic other entity prototype.
    /// </summary>
    public void SetSelectedPrototype(Entity<ChameleonStructureComponent> ent, string? protoId, bool forceUpdate = false)
    {
        // check that wasn't already selected
        // forceUpdate on component init ignores this check
        if (ent.Comp.Default == protoId && !forceUpdate)
            return;

        // make sure that it is valid change
        if (string.IsNullOrEmpty(protoId) || !_proto.TryIndex(protoId, out EntityPrototype? proto))
            return;

        if (!IsValidTarget(proto, ent.Comp.RequireTag))
            return;

        ent.Comp.Default = protoId;
        UpdateVisuals(ent);

        UpdateUi(ent);
        Dirty(ent, ent.Comp);
    }
}
