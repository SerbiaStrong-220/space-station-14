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
}
