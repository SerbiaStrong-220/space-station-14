using Content.Shared.SS220.DarkForces.Narsi.Buildings.Altar;
using Content.Shared.SS220.DarkForces.Narsi.Buildings.Altar.Abilities;

namespace Content.Server.SS220.DarkForces.Narsi.Buildings.Altar;

public sealed partial class NarsiAltarSystem
{
    private void InitAbilities()
    {
        SubscribeLocalEvent<NarsiAltarComponent, NarsiAltarOpenAbilities>(OnOpenAbilities);
        SubscribeLocalEvent<NarsiAltarComponent, NarsiAbilityOpenMessage>(OnTryOpenAbility);
        SubscribeLocalEvent<NarsiAltarComponent, NarsiAbilityLearnMessage>(OnTryLearnAbility);
    }

    private void OnTryLearnAbility(EntityUid uid, NarsiAltarComponent component, NarsiAbilityLearnMessage args)
    {
        _abilities.TryLearnAbility(args.Actor, args.AbilityId);
    }

    private void OnTryOpenAbility(EntityUid uid, NarsiAltarComponent component, NarsiAbilityOpenMessage args)
    {
        if (!_progress.TryOpenAbility(args.AbilityId))
            return;

        _abilities.UpdateAbility(args.AbilityId);
        UpdateAbilitiesState(uid, component, args.Actor);
    }

    private void OnOpenAbilities(EntityUid uid, NarsiAltarComponent component, NarsiAltarOpenAbilities args)
    {
        if (!_ui.TryOpenUi(uid, NarsiAltarAbilitiesInterfaceKey.Key, args.Actor))
            return;

        UpdateAbilitiesState(uid, component, args.Actor);
    }

    private void UpdateAbilitiesState(EntityUid uid, NarsiAltarComponent component, EntityUid? user)
    {
        var state = _progress.GetAbilitiesUIState(user);
        if (state == null)
            return;

        _ui.SetUiState(uid, NarsiAltarAbilitiesInterfaceKey.Key, state);
    }
}
