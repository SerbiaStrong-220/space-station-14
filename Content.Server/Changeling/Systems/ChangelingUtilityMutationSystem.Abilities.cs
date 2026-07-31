// SS220 Changeling
using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Mutations;

namespace Content.Server.Changeling.Systems;

public sealed partial class ChangelingUtilityMutationSystem
{
    private void InitializeUtilityAbilities()
    {
        SubscribeLocalEvent<ChangelingResourceComponent, ChangelingDissonantShriekActionEvent>(OnDissonantShriek);
        SubscribeLocalEvent<ChangelingResourceComponent, ChangelingSpreadInfestationActionEvent>(OnSpreadInfestation);
    }

    private void OnDissonantShriek(Entity<ChangelingResourceComponent> ent, ref ChangelingDissonantShriekActionEvent args)
    {
        if (args.Handled || args.Performer != ent.Owner || !Spend(ent.Owner, 30))
            return;

        args.Handled = true;
        _emp.EmpPulse(Transform(ent).Coordinates, 6f, 100_000f, TimeSpan.FromSeconds(20), ent.Owner);
        _audio.PlayPvs(MutationShriekSound, ent.Owner);
        _popup.PopupEntity(Loc.GetString("changeling-dissonant-shriek-used"), ent.Owner, ent.Owner);
    }

    private void OnSpreadInfestation(
        Entity<ChangelingResourceComponent> ent,
        ref ChangelingSpreadInfestationActionEvent args)
    {
        if (args.Handled ||
            args.Performer != ent.Owner ||
            !TryComp<ChangelingIdentityComponent>(ent, out var identity))
            return;

        if (identity.AbsorbedGenomes.Count < 7)
        {
            _popup.PopupEntity(
                Loc.GetString("changeling-spread-infestation-not-enough-genomes"),
                ent.Owner,
                ent.Owner);
            return;
        }

        if (!Spend(ent.Owner, 45))
            return;

        args.Handled = true;
        Spawn(InfestationSpider, Transform(ent).Coordinates);
        Spawn(InfestationSpider, Transform(ent).Coordinates);
        _popup.PopupEntity(Loc.GetString("changeling-spread-infestation-used"), ent.Owner, ent.Owner);
    }
}
