// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.ChameleonStructure;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.ChameleonStructure;

public sealed class ChameleonStructureSystem : SharedChameleonStructureSystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChameleonStructureComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ChameleonStructureComponent, ChameleonStructurePrototypeSelectedMessage>(OnSelected);
    }

    private void OnMapInit(Entity<ChameleonStructureComponent> ent, ref MapInitEvent args)
    {
        if (string.IsNullOrEmpty(ent.Comp.Prototype))
        {
            ent.Comp.Prototype = MetaData(ent).EntityPrototype?.ID;//Not sure if this secure from null
        }

        SetPrototype(ent, ent.Comp.Prototype, true);
    }

    private void OnSelected(Entity<ChameleonStructureComponent> ent, ref ChameleonStructurePrototypeSelectedMessage args)
    {
        SetPrototype(ent, args.SelectedId);
    }

    private void UpdateUi(Entity<ChameleonStructureComponent> ent)
    {
        var state = new ChameleonStructureBoundUserInterfaceState(ent.Comp.Prototype, ent.Comp.RequireTag);
        UI.SetUiState(ent.Owner, ChameleonStructureUiKey.Key, state);
    }

    /// <summary>
    ///     Change chameleon structure name, description and sprite to mimic other entity prototype.
    /// </summary>
    public void SetPrototype(Entity<ChameleonStructureComponent> ent, string? protoId, bool forceUpdate = false)
    {
        // check that wasn't already selected
        // forceUpdate on component init ignores this check
        if (ent.Comp.Prototype == protoId && !forceUpdate)
            return;

        // make sure that it is valid change
        if (string.IsNullOrEmpty(protoId) || !_proto.TryIndex(protoId, out EntityPrototype? proto))
            return;

        if (!IsValidTarget(proto, ent.Comp.RequireTag))
            return;

        ent.Comp.Prototype = protoId;
        UpdateVisuals(ent);

        UpdateUi(ent);
        Dirty(ent, ent.Comp);

        if (!TryComp<AppearanceComponent>(ent, out var appearance))//it fixes wrong layer states
            return;

        Dirty(ent, appearance);
    }
}
