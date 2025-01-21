// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.CultYogg.Pod;

public abstract class SharedCultYoggPodSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultYoggPodComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<CultYoggPodComponent, CanDropTargetEvent>(OnPodCanDrop);
        SubscribeLocalEvent<CultYoggPodComponent, GetVerbsEvent<AlternativeVerb>>(AddInsertVerb);
        SubscribeLocalEvent<CultYoggPodComponent, AfterPodInserted>(OnPodInsert);
    }

    private void OnPodInsert(Entity<CultYoggPodComponent> ent, ref AfterPodInserted args)
    {
        var xform = Transform(args.User);
        _container.Insert((args.User, xform), ent.Comp.MobContainer);
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
                Text = Loc.GetString("cult-yogg-ensert-pod"),
                Priority = 1
            };

            args.Verbs.Add(verb);
        }
    }

    public bool TryInsert(EntityUid entToEnsert, Entity<CultYoggPodComponent> podEnt)
    {
        if (podEnt.Comp.MobContainer.ContainedEntity != null)
            return false;

        if (!HasComp<MobStateComponent>(entToEnsert) || !HasComp<DamageableComponent>(entToEnsert))
            return false;

        if (_entityWhitelist.IsWhitelistFail(podEnt.Comp.CultistsWhitelist, entToEnsert))
        {
            _popup.PopupClient(Loc.GetString("cult-yogg-heal-only-cultists"), entToEnsert, entToEnsert);

            return false;
        }

        var insertDoAfter = new DoAfterArgs(
            EntityManager,
            entToEnsert,
            podEnt.Comp.InsertDelay,
            new AfterPodInserted(),
            podEnt)
        {
            Broadcast = false,
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = false,

            BlockDuplicate = true,
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent
        };

        return _doAfter.TryStartDoAfter(insertDoAfter);
    }

    public bool TryEject(EntityUid entToEject, Entity<CultYoggPodComponent> podEnt)
    {
        if (podEnt.Comp.MobContainer.ContainedEntity is null)
            return false;

        _container.Remove(entToEject, podEnt.Comp.MobContainer);

        return true;
    }
}

[Serializable, NetSerializable]
public sealed partial class AfterPodInserted : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}
