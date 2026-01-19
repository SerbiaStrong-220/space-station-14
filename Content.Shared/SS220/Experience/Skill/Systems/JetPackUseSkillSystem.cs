// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.SS220.Experience.Skill.Components;
using Robust.Shared.Random;

namespace Content.Shared.SS220.Experience.Skill.Systems;

public sealed class JetPackUseSkillSystem : SkillEntitySystem
{
    [Dependency] private readonly SharedJetpackSystem _jetpack = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeEventToSkillEntity<JetPackUseSkillComponent, RefreshWeightlessModifiersEvent>(OnRefreshWeightlessModifiers);
        SubscribeEventToSkillEntity<JetPackUseSkillComponent, JetPackActivatedEvent>(OnJetPackActivated);
        SubscribeEventToSkillEntity<JetPackUseSkillComponent, MoveInputEvent>(OnMoveInput);
    }

    private void OnRefreshWeightlessModifiers(Entity<JetPackUseSkillComponent> entity, ref RefreshWeightlessModifiersEvent args)
    {
        if (!ResolveExperienceEntityFromSkillEntity(entity, out var experienceEntity))
            return;

        if (!_jetpack.IsUserFlying(experienceEntity.Value.Owner))
            return;

        args.WeightlessAcceleration = entity.Comp.WeightlessAcceleration;
        args.WeightlessModifier = entity.Comp.WeightlessModifier;
        args.WeightlessFriction = entity.Comp.WeightlessFriction;
        args.WeightlessFrictionNoInput = entity.Comp.WeightlessFrictionNoInput;
    }

    private void OnJetPackActivated(Entity<JetPackUseSkillComponent> entity, ref JetPackActivatedEvent args)
    {
        if (args.JetPack.Comp is null)
        {
            entity.Comp.JetPackActive = false;
            return;
        }

        entity.Comp.JetPackActive = true;
        args.JetPack.Comp.GasUsageModifier = entity.Comp.GasUsageModifier;
    }

    private void OnMoveInput(Entity<JetPackUseSkillComponent> entity, ref MoveInputEvent args)
    {
        if (!ResolveExperienceEntityFromSkillEntity(entity, out var experienceEntity)) return;

        if (!TryComp<JetpackUserComponent>(experienceEntity, out var userComponent)) return;

        if (!TryComp<JetpackComponent>(userComponent.Jetpack, out var jetpackComponent)) return;

        if (!HasComp<ActiveJetpackComponent>(userComponent.Jetpack)) return;

        if (jetpackComponent.ToggleActionEntity is not { } actionUid) return;

        if (_actions.GetAction(actionUid) is not { } actionEntity) return;

        if (!args.HasDirectionalMovement) return;

        var predictedRandom = GetPredictedRandom(new() { GetNetEntity(entity).Id });

        if (!predictedRandom.Prob(entity.Comp.FailChance))
            return;

        _actions.SetCooldown(actionEntity!, entity.Comp.JetPackFailureTime);
        _actions.PerformAction(experienceEntity.Value.Owner, actionEntity);
        _popup.PopupCursor(Loc.GetString(entity.Comp.JetPackFailurePopup), experienceEntity.Value.Owner, PopupType.MediumCaution);
    }
}
