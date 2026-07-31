// SS220 Changeling
using Content.Server.Changeling.Components;
using Content.Shared.Changeling;
using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Mutations;
using Content.Shared.Changeling.Systems;
using Content.Shared.Cloning;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Robust.Shared.GameObjects;

namespace Content.Server.Changeling.Systems;

public sealed partial class ChangelingUtilityMutationSystem
{
    private void InitializeTransformationSting()
    {
        SubscribeLocalEvent<ChangelingResourceComponent, ChangelingTransformationStingActionEvent>(OnTransformationSting);
        SubscribeLocalEvent<ChangelingResourceComponent, ChangelingTransformationStingIdentitySelectMessage>(OnTransformationIdentitySelected);
        SubscribeLocalEvent<ChangelingResourceComponent, ChangelingTransformationStingDoAfterEvent>(OnTransformationStingFinished);
        SubscribeLocalEvent<ChangelingTransformationStingComponent, ComponentShutdown>(OnTransformationStingShutdown);
    }

    private void UpdateTransformationStings(TimeSpan now)
    {
        var query = EntityQueryEnumerator<ChangelingTransformationStingComponent>();
        while (query.MoveNext(out var uid, out var transformation))
        {
            if (now >= transformation.EndTime)
                RestoreTransformationSting(uid, transformation);
        }
    }

    private void OnTransformationSting(
        Entity<ChangelingResourceComponent> ent,
        ref ChangelingTransformationStingActionEvent args)
    {
        if (args.Handled ||
            args.Performer != ent.Owner ||
            !IsValidChemicalAmount(args.ChemicalCost) ||
            !float.IsFinite(args.StingRange) ||
            !TryComp<ChangelingIdentityComponent>(ent, out var identity))
        {
            return;
        }

        var state = EnsureState(ent);
        if (state.TransformationStingInProgress)
        {
            _popup.PopupEntity(
                Loc.GetString("changeling-transformation-sting-already-preparing"),
                ent.Owner,
                ent.Owner);
            return;
        }

        if (identity.ConsumedIdentities.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("changeling-transformation-sting-no-identities"), ent.Owner, ent.Owner);
            return;
        }

        var stingRange = Math.Clamp(
            args.StingRange,
            0.1f,
            SharedInteractionSystem.InteractionRange);
        if (!IsValidTransformationTarget(ent.Owner, args.Target, stingRange, showPopup: true))
            return;

        state.PendingTransformationTarget = args.Target;
        state.PendingTransformationIdentity = null;
        state.PendingTransformationChemicalCost = Math.Max(0f, args.ChemicalCost);
        state.PendingTransformationRange = stingRange;
        state.PendingTransformationWindup = TimeSpan.FromTicks(Math.Clamp(
            args.StingWindup.Ticks,
            0,
            MaxTransformationWindup.Ticks));
        state.PendingTransformationDuration = TimeSpan.FromTicks(Math.Clamp(
            args.TransformDuration.Ticks,
            1,
            MaxTransformationDuration.Ticks));

        var userInterface = EnsureComp<UserInterfaceComponent>(ent);
        _ui.SetUi(
            (ent.Owner, userInterface),
            ChangelingTransformUiKey.TransformationSting,
            new InterfaceData(ChangelingBuiXmlGeneratedName));
        _ui.OpenUi(
            (ent.Owner, userInterface),
            ChangelingTransformUiKey.TransformationSting,
            ent.Owner);
        args.Handled = true;
    }

    private void OnTransformationIdentitySelected(
        Entity<ChangelingResourceComponent> ent,
        ref ChangelingTransformationStingIdentitySelectMessage args)
    {
        if (args.Actor != ent.Owner ||
            !TryComp<UserInterfaceComponent>(ent, out var userInterface) ||
            !_ui.IsUiOpen(
                (ent.Owner, userInterface),
                ChangelingTransformUiKey.TransformationSting,
                args.Actor))
        {
            return;
        }

        if (!TryGetEntity(args.TargetIdentity, out var selectedIdentity) ||
            !TryComp<ChangelingIdentityComponent>(ent, out var identities) ||
            !TryComp<ChangelingUtilityStateComponent>(ent, out var state) ||
            state.PendingTransformationTarget is not { } target ||
            state.TransformationStingInProgress)
        {
            return;
        }

        if (!identities.ConsumedIdentities.ContainsKey(selectedIdentity.Value) ||
            !HasComp<ChangelingStoredIdentityComponent>(selectedIdentity.Value) ||
            !IsValidTransformationTarget(ent.Owner, target, state.PendingTransformationRange, showPopup: true))
        {
            ClearPendingTransformation(ent.Owner, state);
            return;
        }

        state.PendingTransformationIdentity = selectedIdentity.Value;
        state.TransformationStingInProgress = true;
        _ui.CloseUi(
            (ent.Owner, userInterface),
            ChangelingTransformUiKey.TransformationSting,
            ent.Owner);

        var doAfter = new DoAfterArgs(
            EntityManager,
            ent.Owner,
            state.PendingTransformationWindup,
            new ChangelingTransformationStingDoAfterEvent(),
            ent.Owner,
            target: target)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            BlockDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent,
            DistanceThreshold = state.PendingTransformationRange,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
        {
            ClearPendingTransformation(ent.Owner, state);
            return;
        }

        _popup.PopupEntity(Loc.GetString("changeling-transformation-sting-preparing"), ent.Owner, ent.Owner);
    }

    private void OnTransformationStingFinished(
        Entity<ChangelingResourceComponent> ent,
        ref ChangelingTransformationStingDoAfterEvent args)
    {
        if (args.Handled || !TryComp<ChangelingUtilityStateComponent>(ent, out var state))
            return;

        args.Handled = true;
        var target = state.PendingTransformationTarget;
        var selectedIdentity = state.PendingTransformationIdentity;
        var chemicalCost = state.PendingTransformationChemicalCost;
        var transformDuration = state.PendingTransformationDuration;
        var range = state.PendingTransformationRange;
        ClearPendingTransformation(ent.Owner, state);

        if (args.Cancelled ||
            target is not { } victim ||
            selectedIdentity is not { } identity ||
            !IsValidChemicalAmount(chemicalCost))
        {
            return;
        }

        if (!TryComp<ChangelingIdentityComponent>(ent, out var identities) ||
            !identities.ConsumedIdentities.ContainsKey(identity) ||
            !HasComp<ChangelingStoredIdentityComponent>(identity) ||
            !IsValidTransformationTarget(ent.Owner, victim, range, showPopup: true))
        {
            return;
        }

        if (!_resources.TrySpendChemicals(ent.Owner, FixedPoint2.New(chemicalCost)))
        {
            _popup.PopupEntity(Loc.GetString("changeling-not-enough-chemicals"), ent.Owner, ent.Owner);
            return;
        }

        if (!_prototype.Resolve(identities.IdentityCloningSettings, out var cloningSettings) ||
            _identities.CloneToPausedMap(cloningSettings, victim) is not { } backup)
        {
            _resources.AddChemicals(ent.Owner, FixedPoint2.New(chemicalCost));
            _popup.PopupEntity(Loc.GetString("changeling-transformation-sting-failed"), ent.Owner, ent.Owner);
            return;
        }

        var transformation = new ChangelingTransformationStingComponent
        {
            Backup = backup,
            EndTime = _timing.CurTime + transformDuration,
            CloningSettings = identities.IdentityCloningSettings,
        };
        AddComp(victim, transformation);

        var beforeTransform = new BeforeChangelingTransformEvent(identity);
        RaiseLocalEvent(victim, beforeTransform);
        _visualBody.CopyAppearanceFrom(identity, victim);
        _cloning.CloneComponents(identity, victim, cloningSettings);
        _metaData.SetEntityName(victim, Name(identity), raiseEvents: false);
        _identitySystem.QueueIdentityUpdate(victim);
        var afterTransform = new AfterChangelingTransformEvent(identity);
        RaiseLocalEvent(victim, afterTransform);

        _popup.PopupEntity(Loc.GetString("changeling-transformation-sting-success"), ent.Owner, ent.Owner);
    }

    private bool IsValidTransformationTarget(EntityUid owner, EntityUid target, float range, bool showPopup)
    {
        var valid = !TerminatingOrDeleted(owner) &&
                    owner != target &&
                    !TerminatingOrDeleted(target) &&
                    HasComp<HumanoidProfileComponent>(target) &&
                    _mobState.IsAlive(target) &&
                    !HasComp<ChangelingIdentityComponent>(target) &&
                    !HasComp<ChangelingLesserFormComponent>(target) &&
                    !HasComp<ChangelingTransformationStingComponent>(target) &&
                    _interaction.InRangeUnobstructed(owner, target, range: Math.Max(0.1f, range));
        if (!valid && showPopup)
        {
            _popup.PopupEntity(
                Loc.GetString("changeling-transformation-sting-invalid-target"),
                owner,
                owner);
        }

        return valid;
    }

    private void ClearPendingTransformation(EntityUid uid, ChangelingUtilityStateComponent state)
    {
        state.PendingTransformationTarget = null;
        state.PendingTransformationIdentity = null;
        state.PendingTransformationChemicalCost = 0f;
        state.PendingTransformationRange = 0f;
        state.PendingTransformationWindup = TimeSpan.Zero;
        state.PendingTransformationDuration = TimeSpan.Zero;
        state.TransformationStingInProgress = false;
        if (TryComp<UserInterfaceComponent>(uid, out var userInterface))
        {
            _ui.CloseUi(
                (uid, userInterface),
                ChangelingTransformUiKey.TransformationSting,
                uid);
        }
    }

    private void RestoreTransformationSting(
        EntityUid uid,
        ChangelingTransformationStingComponent transformation)
    {
        if (transformation.Backup is not { } backup || !Exists(backup))
        {
            Log.Error($"Unable to restore Transformation Sting victim {ToPrettyString(uid)}: its identity backup is missing.");
            transformation.Backup = null;
            RemComp<ChangelingTransformationStingComponent>(uid);
            return;
        }

        if (!_prototype.Resolve(transformation.CloningSettings, out var cloningSettings))
        {
            Log.Error($"Unable to restore Transformation Sting victim {ToPrettyString(uid)}: cloning settings are invalid.");
            transformation.Backup = null;
            QueueDel(backup);
            RemComp<ChangelingTransformationStingComponent>(uid);
            return;
        }

        RestoreTransformationAppearance(uid, backup, cloningSettings);

        transformation.Backup = null;
        QueueDel(backup);
        RemComp<ChangelingTransformationStingComponent>(uid);
    }

    private void RestoreTransformationAppearance(
        EntityUid uid,
        EntityUid backup,
        CloningSettingsPrototype cloningSettings)
    {
        var beforeTransform = new BeforeChangelingTransformEvent(backup);
        RaiseLocalEvent(uid, beforeTransform);
        _visualBody.CopyAppearanceFrom(backup, uid);
        _cloning.CloneComponents(backup, uid, cloningSettings);
        _metaData.SetEntityName(uid, Name(backup), raiseEvents: false);
        _identitySystem.QueueIdentityUpdate(uid);
        var afterTransform = new AfterChangelingTransformEvent(backup);
        RaiseLocalEvent(uid, afterTransform);
    }

    private void OnTransformationStingShutdown(
        Entity<ChangelingTransformationStingComponent> ent,
        ref ComponentShutdown args)
    {
        if (ent.Comp.Backup is not { } backup)
            return;

        ent.Comp.Backup = null;
        if (!TerminatingOrDeleted(ent.Owner) &&
            Exists(backup) &&
            _prototype.Resolve(ent.Comp.CloningSettings, out var cloningSettings))
        {
            // External component removal must not leave a living victim permanently disguised.
            RestoreTransformationAppearance(ent.Owner, backup, cloningSettings);
        }

        QueueDel(backup);
    }
}
