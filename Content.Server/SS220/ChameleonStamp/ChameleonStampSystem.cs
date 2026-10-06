// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.ChameleonStamp;

namespace Content.Server.SS220.ChameleonStamp;

public sealed partial class ChameleonStampSystem : SharedChameleonStampSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChameleonStampComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ChameleonStampComponent, ChameleonStampSelectedMessage>(OnSelected);
    }

    private void OnMapInit(Entity<ChameleonStampComponent> ent, ref MapInitEvent args)
    {
        TrySetPrototype(ent, ent.Comp.Prototype, true);
    }

    private void OnSelected(Entity<ChameleonStampComponent> ent, ref ChameleonStampSelectedMessage args)
    {
        TrySetPrototype(ent, args.SelectedId);
    }
}