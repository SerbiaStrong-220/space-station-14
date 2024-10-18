// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Content.Shared.DragDrop;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.SS220.CultYogg.Components;
using Robust.Shared.Containers;
using Robust.Shared.Network;

namespace Content.Shared.SS220.CultYogg.EntitySystems;

public abstract class SharedCultYoggPodSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultYoggPodComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<CultYoggPodComponent, CanDropTargetEvent>(OnPodCanDrop);
    }

    private void OnPodCanDrop(Entity<CultYoggPodComponent> ent, ref CanDropTargetEvent args)
    {
        if (args.Handled)
            return;

        args.CanDrop = HasComp<DamageableComponent>(args.Dragged);
        args.Handled = true;
    }

    private void OnCompInit(Entity<CultYoggPodComponent> ent, ref ComponentInit args)
    {
        ent.Comp.MobContainer = _container.EnsureContainer<ContainerSlot>(ent, "cultyYoggPod");
    }

    public bool TryInsert(EntityUid entToEnsert, Entity<CultYoggPodComponent> podEnt)
    {
        if (podEnt.Comp.MobContainer.ContainedEntity != null)
            return false;

        if (!HasComp<MobStateComponent>(entToEnsert) || !HasComp<DamageableComponent>(entToEnsert))
            return false;

        if (!HasComp<CultYoggComponent>(entToEnsert))
        {
            if (_net.IsServer)
                _popup.PopupEntity(Loc.GetString("cult-yogg-heal-only-cultists"), entToEnsert, entToEnsert);

            return false;
        }

        var xform = Transform(entToEnsert);

        _container.Insert((entToEnsert, xform), podEnt.Comp.MobContainer);

        return true;
    }

    public bool TryEject(EntityUid entToEject, Entity<CultYoggPodComponent> podEnt)
    {
        if (podEnt.Comp.MobContainer.ContainedEntity is null)
            return false;

        _container.Remove(entToEject, podEnt.Comp.MobContainer);

        return true;
    }
}
