using Content.Shared.DoAfter;
using Content.Shared.Forensics.Components;
using Content.Shared.Humanoid;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.SS220.PenScrambler;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.PenScrambler;

public sealed class PenScramblerSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly MetaDataSystem _metaSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PenScramblerComponent, AfterInteractEvent>(OnInteract);
        SubscribeLocalEvent<PenScramblerComponent, CopyDnaToPenEvent>(OnCopyIdentity);
    }

    private void OnInteract(Entity<PenScramblerComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target == null)
            return;

        if (HasComp<HumanoidAppearanceComponent>(args.Target) && !ent.Comp.HaveDna)
        {
            _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
                args.User,
                ent.Comp.DelayForExtractDna,
                new CopyDnaToPenEvent(),
                ent.Owner,
                args.Target)
            {
                Hidden = true,
                BreakOnMove = true,
                BreakOnDamage = true,
                BreakOnHandChange = true,
                BreakOnDropItem = true,
                DuplicateCondition = DuplicateConditions.None,
            });
        }

        if (!TryComp<ImplanterComponent>(args.Target, out var implanterComponent))
            return;

        var implantEntity = implanterComponent.ImplanterSlot.ContainerSlot?.ContainedEntity;

        if (HasComp<TransferIdentityComponent>(implantEntity) && ent.Comp.HaveDna)
        {
            _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
                args.User,
                ent.Comp.DelayForTransferToImplant,
                new CopyDnaFromPenToImplantEvent(),
                implantEntity,
                ent.Owner)
            {
                BreakOnMove = true,
                BreakOnDamage = true,
                BreakOnHandChange = true,
                BreakOnDropItem = true,
                DuplicateCondition = DuplicateConditions.None,
            });
        }
    }

    EntityUid? CloneToNullspace(Entity<PenScramblerComponent> ent, EntityUid target)
    {
        if (!TryComp<HumanoidAppearanceComponent>(target, out var humanoid)
            || !_prototype.TryIndex(humanoid.Species, out var speciesPrototype)
            || !TryComp<DnaComponent>(target, out var targetDna))
            return null;

        var mob = Spawn(speciesPrototype.Prototype, MapCoordinates.Nullspace);

        _humanoidSystem.CloneAppearance(target, mob);

        if (!TryComp<DnaComponent>(mob, out var mobDna))
            return null;

        mobDna.DNA = targetDna.DNA;

        _metaSystem.SetEntityName(mob, Name(target));
        _metaSystem.SetEntityDescription(mob, MetaData(target).EntityDescription);

        SetPaused(mob, true);

        return mob;
    }

    private void OnCopyIdentity(Entity<PenScramblerComponent> ent, ref CopyDnaToPenEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Target is not { } target)
            return;

        // Create a nullspace clone of the target to copy from later
        // so we get an expected result even if target gets DNA scrambled / gibbed
        EntityUid? mob = CloneToNullspace(ent, target);

        if (mob == null)
            return;

        ent.Comp.NullspaceClone = mob;
        ent.Comp.HaveDna = true;

        _popup.PopupEntity(Loc.GetString("pen-scrambler-success-copy", ("identity", MetaData(args.Target.Value).EntityName)), args.User, args.User);
    }
}
