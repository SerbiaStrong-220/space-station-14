using Content.Server.Polymorph.Systems;
using Content.Server.SS220.DarkForces.Narsi.Buildings.Altar.Rituals.Polymorth;

namespace Content.Server.SS220.DarkForces.Narsi.Buildings.Altar;

public sealed partial class NarsiAltarSystem
{
    private void InitPolymorph()
    {
        SubscribeLocalEvent<NarsiAltarComponent, NarsiRequestPolymorphEvent>(OnPolymorphRequest);
        SubscribeLocalEvent<NarsiPolymorphComponent, PolymorphRevertedEvent>(OnPolymorpgReverted);
    }

    private void OnPolymorphRequest(EntityUid uid, NarsiAltarComponent component, NarsiRequestPolymorphEvent args)
    {
        var polymorphEntity = _polymorph.PolymorphEntity(args.Target, args.Configuration);
        if (polymorphEntity == null)
            return;

        var polymorphComponent = EnsureComp<NarsiPolymorphComponent>(polymorphEntity.Value);
        polymorphComponent.AltarEntityUid = uid;
        polymorphComponent.ReturnToAltar = args.ReturnToAltar;
    }

    private void OnPolymorpgReverted(EntityUid uid, NarsiPolymorphComponent component, PolymorphRevertedEvent args)
    {
        var altar = component.AltarEntityUid;
        if (!component.ReturnToAltar || !EntityManager.EntityExists(altar))
            return;

        var transform = Transform(altar);
        _transform.SetCoordinates(args.Original, transform.Coordinates);
    }
}
