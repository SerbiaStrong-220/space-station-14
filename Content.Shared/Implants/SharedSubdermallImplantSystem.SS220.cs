// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.PenScrambler;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Implants.Components;
using Content.Shared.Humanoid;
using Content.Shared.Forensics.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Forensics;
using Content.Shared.Charges.Components;

namespace Content.Shared.Implants;

// TODO-SS220 move all this into own trigger based like wizden done (ex. UncuffOnTriggerSystem.cs)
public abstract partial class SharedSubdermalImplantSystem : EntitySystem // SS220 move out code into partial
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IdentitySystem _identity = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private const string BeakerSolution = "beaker"; //ss220 add adrenal implant
    private const string ChemicalSolution = "chemicals"; //ss220 add adrenal implant

    // SS220 - chemical-implants start
    private void OnChemicalImplant(EntityUid uid, SubdermalImplantComponent component, UseChemicalImplantEvent args)
    {
        if (component.ImplantedEntity is not { } ent)
            return;
        if (!TryComp<SolutionContainerManagerComponent>(args.Performer, out var performerSolutionComp)
            || !TryComp<SolutionContainerManagerComponent>(uid, out var implantSolutionComp))
            return;

        if (!_solutionContainer.TryGetSolution(new(args.Performer, performerSolutionComp), "chemicals", out var chemicalSolution))
            return;

        if (!_solutionContainer.TryGetSolution(new(uid, implantSolutionComp), "beaker", out var beakerSolution))
            return;

        var transferAmount = beakerSolution.Value.Comp.Solution.Volume;
        if (TryComp<LimitedChargesComponent>(args.Action, out var limitedCharges))
            transferAmount /= limitedCharges.MaxCharges;

        _solutionContainer.TryTransferSolution(chemicalSolution.Value, beakerSolution.Value.Comp.Solution, transferAmount);

        args.Handled = true;

    }
    //ss220 dna copy implant add start
    private void OnDnaCopyImplant(Entity<SubdermalImplantComponent> ent, ref UseDnaCopyImplantEvent args)
    {
        if (!TryComp<TransferIdentityComponent>(ent.Owner, out var transferIdentityComponent))
            return;

        var clone = transferIdentityComponent.NullspaceClone;

        if (clone == null)
        {
            QueueDel(ent);
            return;
        }

        if (ent.Comp.ImplantedEntity is not { } user)
            return;

        if (TryComp<HumanoidAppearanceComponent>(user, out var userAppearanceComp))
        {

            if (!TryComp<HumanoidAppearanceComponent>(clone, out var cloneAppearanceComp))
                return;

            _humanoidAppearance.CloneAppearance(clone.Value, user, cloneAppearanceComp, userAppearanceComp);

            _metaData.SetEntityName(user, MetaData(clone.Value).EntityName, raiseEvents: false);

            if (TryComp<DnaComponent>(user, out var dna)
                && TryComp<DnaComponent>(clone.Value, out var dnaClone) &&
                dnaClone.DNA != null)
            {
                dna.DNA = dnaClone.DNA;
                var ev = new GenerateDnaEvent { Owner = user, DNA = dna.DNA };
                RaiseLocalEvent(ent, ref ev);
                Dirty(user, dna);
            }

            if (TryComp<FingerprintComponent>(user, out var fingerprint)
                && TryComp<FingerprintComponent>(clone.Value, out var fingerprintTarget))
            {
                fingerprint.Fingerprint = fingerprintTarget.Fingerprint;
                Dirty(user, fingerprint);
            }

            var setScale = EnsureComp<SetScaleFromTargetComponent>(user);
            setScale.Target = GetNetEntity(clone);

            Dirty(user, setScale);

            var evEvent = new SetScaleFromTargetEvent(GetNetEntity(user), setScale.Target);
            RaiseNetworkEvent(evEvent);

            _identity.QueueIdentityUpdate(user);

            _popup.PopupEntity(Loc.GetString("pen-scrambler-success-convert-to-identity", ("identity", MetaData(clone.Value).EntityName)), user, user);
        }

        QueueDel(clone);
        QueueDel(ent);
    }
    //ss220 dna copy implant add end

    //ss220 add adrenal implant start
    private void OnAdrenalImplant(Entity<SubdermalImplantComponent> ent, ref UseAdrenalImplantEvent args)
    {
        if (!TryComp<SolutionContainerManagerComponent>(ent.Owner, out var solutionImplantComp))
            return;

        if (!TryComp<SolutionContainerManagerComponent>(args.Performer, out var solutionUserComp))
            return;

        if (!_solutionContainer.TryGetSolution((ent.Owner, solutionImplantComp), BeakerSolution, out var solutionImplant))
            return;

        if (!_solutionContainer.TryGetSolution((args.Performer, solutionUserComp), ChemicalSolution, out var solutionUser))
            return;

        var quantity = solutionImplant.Value.Comp.Solution.Volume;
        if (TryComp<LimitedChargesComponent>(args.Action, out var actionCharges))
            quantity /= actionCharges.MaxCharges;

        _solutionContainer.TryTransferSolution(solutionUser.Value,
            solutionImplant.Value.Comp.Solution,
            quantity);

        args.Handled = true;
    }
    //ss220 add adrenal implant end
}
