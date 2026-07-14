// SS220 Changeling
using Content.Shared.Actions;
using Content.Shared.Changeling;
using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Mutations;
using Content.Shared.Changeling.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Popups;

namespace Content.Server.Changeling.Systems;

/// <summary>
/// Handles the changeling's silent extraction sting. The action deliberately produces no popup for the
/// victim; failed stings only inform the changeling.
/// </summary>
public sealed class ChangelingExtractDnaSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedChangelingIdentitySystem _identity = default!;
    [Dependency] private readonly ChangelingResourceSystem _resources = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ChangelingExtractDnaComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ChangelingExtractDnaComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ChangelingExtractDnaComponent, ChangelingExtractDnaActionEvent>(OnExtract);
    }

    private void OnStartup(Entity<ChangelingExtractDnaComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.ChemicalCost = Math.Max(0, ent.Comp.ChemicalCost);
        _actions.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.Action);
    }

    private void OnShutdown(Entity<ChangelingExtractDnaComponent> ent, ref ComponentShutdown args)
    {
        if (!TerminatingOrDeleted(ent.Owner) && ent.Comp.ActionEntity is { } action)
            _actions.RemoveAction(ent.Owner, action);
    }

    private void OnExtract(Entity<ChangelingExtractDnaComponent> ent, ref ChangelingExtractDnaActionEvent args)
    {
        if (args.Handled ||
            args.Performer != ent.Owner ||
            args.Target == ent.Owner ||
            !HasComp<HumanoidProfileComponent>(args.Target))
            return;

        args.Handled = true;

        var chemicalCost = FixedPoint2.New(Math.Max(0, ent.Comp.ChemicalCost));
        if (!_resources.TrySpendChemicals(ent.Owner, chemicalCost))
        {
            _popup.PopupEntity(Loc.GetString("changeling-not-enough-chemicals"), ent.Owner, ent.Owner);
            return;
        }

        // Changelings are immune. The cost is still paid, making a failed silent sting the intended tell.
        if (HasComp<ChangelingIdentityComponent>(args.Target) || HasComp<ChangelingLesserFormComponent>(args.Target))
        {
            _popup.PopupEntity(Loc.GetString("changeling-extract-dna-target-is-changeling"), ent.Owner, ent.Owner);
            return;
        }

        if (!TryComp<ChangelingIdentityComponent>(ent.Owner, out var storage)
            || !_identity.TryStoreIdentity((ent.Owner, storage), args.Target, countForObjective: true, out _))
        {
            _resources.AddChemicals(ent.Owner, chemicalCost);
            _popup.PopupEntity(Loc.GetString("changeling-extract-dna-storage-full"), ent.Owner, ent.Owner);
            return;
        }

        _popup.PopupEntity(Loc.GetString("changeling-extract-dna-success"), ent.Owner, ent.Owner);
    }
}
