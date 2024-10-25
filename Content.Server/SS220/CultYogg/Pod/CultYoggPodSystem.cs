// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DragDrop;
using Content.Shared.SS220.CultYogg.Pod;
using Content.Shared.SS220.CultYogg.MiGo;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Server.GameObjects;

namespace Content.Server.SS220.CultYogg.Pod;

public sealed partial class CultYoggPodSystem : SharedCultYoggPodSystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultYoggPodComponent, EntInsertedIntoContainerMessage>(GotInserted);
        SubscribeLocalEvent<CultYoggPodComponent, EntRemovedFromContainerMessage>(GotRemoved);
        SubscribeLocalEvent<CultYoggPodComponent, EntityTerminatingEvent>(GotTerminated);
        SubscribeLocalEvent<CultYoggPodComponent, DragDropTargetEvent>(OnCanDropHandle);
        SubscribeLocalEvent<CultYoggPodComponent, GetVerbsEvent<AlternativeVerb>>(AddInsertVerb);
    }

    private void AddInsertVerb(Entity<CultYoggPodComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var target = args.User;

        if (ent.Comp.MobContainer.ContainedEntity is not null)
        {
            AlternativeVerb verb = new()
            {
                Act = () => TryEject(ent.Comp.MobContainer.ContainedEntity.Value, ent),
                Category = VerbCategory.Eject,
                Text = Loc.GetString("cult-yogg-eject-pod"),
                Priority = 1
            };

            args.Verbs.Add(verb);
        }

        if (ent.Comp.MobContainer.ContainedEntity is null)
        {
            AlternativeVerb verb = new()
            {
                Act = () => TryInsert(target, ent),
                Category = VerbCategory.Insert,
                Text = Loc.GetString("cult-yogg-ensert-pod"),
                Priority = 1
            };

            args.Verbs.Add(verb);
        }
    }

    private void OnCanDropHandle(Entity<CultYoggPodComponent> ent, ref DragDropTargetEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryInsert(args.Dragged, ent);
    }

    private void GotTerminated(Entity<CultYoggPodComponent> ent, ref EntityTerminatingEvent args)
    {
        if (ent.Comp.MobContainer.ContainedEntity is null)
            return;

        RemComp<CultYoggHealComponent>(ent.Comp.MobContainer.ContainedEntity.Value);
    }

    private void GotRemoved(Entity<CultYoggPodComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        RemComp<CultYoggHealComponent>(args.Entity);
        _appearance.SetData(ent, CultYoggPodComponent.CultPodVisuals.Inserted, false);
    }

    private void GotInserted(Entity<CultYoggPodComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        EnsureComp<CultYoggHealComponent>(args.Entity);
        _appearance.SetData(ent, CultYoggPodComponent.CultPodVisuals.Inserted, true);
    }
}
