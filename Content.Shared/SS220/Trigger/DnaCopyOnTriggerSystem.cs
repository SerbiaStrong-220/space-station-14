using Content.Shared.Forensics;
using Content.Shared.Forensics.Components;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.SS220.PenScrambler;
using Content.Shared.Trigger;

namespace Content.Shared.SS220.Trigger;

public sealed class DnaCopyOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IdentitySystem _identity = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<DnaCopyOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<DnaCopyOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target == null)
            return;

        if (!TryComp<TransferIdentityComponent>(ent.Owner, out var transferIdentityComponent))
            return;

        var clone = transferIdentityComponent.NullspaceClone;

        if (clone == null)
        {
            QueueDel(ent);
            return;
        }

        if (TryComp<HumanoidAppearanceComponent>(target, out var userAppearanceComp))
        {

            if (!TryComp<HumanoidAppearanceComponent>(clone, out var cloneAppearanceComp))
                return;

            _humanoidAppearance.CloneAppearance(clone.Value, target.Value, cloneAppearanceComp, userAppearanceComp);

            _metaData.SetEntityName(target.Value, MetaData(clone.Value).EntityName, raiseEvents: false);

            if (TryComp<DnaComponent>(target, out var dna)
                && TryComp<DnaComponent>(clone.Value, out var dnaClone) &&
                dnaClone.DNA != null)
            {
                dna.DNA = dnaClone.DNA;
                var ev = new GenerateDnaEvent { Owner = target.Value, DNA = dna.DNA };
                RaiseLocalEvent(ent, ref ev);
                Dirty(target.Value, dna);
            }

            if (TryComp<FingerprintComponent>(target, out var fingerprint)
                && TryComp<FingerprintComponent>(clone.Value, out var fingerprintTarget))
            {
                fingerprint.Fingerprint = fingerprintTarget.Fingerprint;
                Dirty(target.Value, fingerprint);
            }

            var setScale = EnsureComp<SetScaleFromTargetComponent>(target.Value);
            setScale.Target = GetNetEntity(clone);

            Dirty(target.Value, setScale);

            var evEvent = new SetScaleFromTargetEvent(GetNetEntity(target.Value), setScale.Target);
            RaiseNetworkEvent(evEvent);

            _identity.QueueIdentityUpdate(target.Value);

            _popup.PopupEntity(Loc.GetString("pen-scrambler-success-convert-to-identity", ("identity", MetaData(clone.Value).EntityName)), target.Value, target.Value);
        }

        QueueDel(clone);
        QueueDel(ent);
    }
}
