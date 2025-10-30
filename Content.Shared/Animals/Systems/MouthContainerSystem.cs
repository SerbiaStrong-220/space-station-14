using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Animals.Components;
using Content.Shared.Gibbing.Events;
namespace Content.Shared.Animals.Systems;

public sealed class MouthContainerSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MouthContainerComponent, EntityGibbedEvent>(OnEntityGibbedEvent);
    }


    private void OnEntityGibbedEvent(Entity<MouthContainerComponent> ent, ref EntityGibbedEvent args)
    {
        _popup.PopupEntity("Death", ent.Owner);
    }
}
