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

        SubscribeLocalEvent<ChameleonStructureComponent, ChameleonStructurePrototypeSelectedMessage>(OnSelected);
    }

    private void OnSelected(Entity<ChameleonStructureComponent> ent, ref ChameleonStructurePrototypeSelectedMessage args)
    {
        SetPrototype(ent, args.SelectedId);
    }
}
