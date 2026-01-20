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

    private EntityQuery<JetpackComponent> _jetpackQuery;
    private EntityQuery<JetpackUserComponent> _jetpackUserQuery;
    private EntityQuery<ActiveJetpackComponent> _activeJetpackQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeEventToSkillEntity<JetPackUseSkillComponent, RefreshWeightlessModifiersEvent>(OnRefreshWeightlessModifiers);
        SubscribeEventToSkillEntity<JetPackUseSkillComponent, JetPackActivatedEvent>(OnJetPackActivated);
        SubscribeEventToSkillEntity<JetPackUseSkillComponent, MoveInputEvent>(OnMoveInput);

        _jetpackQuery = GetEntityQuery<JetpackComponent>();
        _jetpackUserQuery = GetEntityQuery<JetpackUserComponent>();
        _activeJetpackQuery = GetEntityQuery<ActiveJetpackComponent>();
    }

    private void OnRefreshWeightlessModifiers(Entity<JetPackUseSkillComponent> entity, ref RefreshWeightlessModifiersEvent args)
    {
        if (!ResolveExperienceEntityFromSkillEntity(entity, out var experienceEntity))
            return;

        if (!_jetpack.IsUserFlying(experienceEntity.Value.Owner))
            return;

        args.WeightlessAcceleration *= entity.Comp.WeightlessAcceleration;
        args.WeightlessModifier *= entity.Comp.WeightlessModifier;
        args.WeightlessFriction *= entity.Comp.WeightlessFriction;
        args.WeightlessFrictionNoInput *= entity.Comp.WeightlessFrictionNoInput;
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
        if (!_jetpackUserQuery.TryGetComponent(args.Entity, out var userComponent)) return;

        if (!_jetpackQuery.TryGetComponent(userComponent.Jetpack, out var jetpackComponent)) return;

        if (!_jetpackQuery.HasComp(userComponent.Jetpack)) return;

        if (jetpackComponent.ToggleActionEntity is not { } actionUid) return;

        if (_actions.GetAction(actionUid) is not { } actionEntity) return;

        if (!args.HasDirectionalMovement) return;

        var predictedRandom = GetPredictedRandomDebug(new() { GetNetEntity(entity).Id }, out var seed);

        if (!predictedRandom.Prob(entity.Comp.FailChance))
        {
            Log.Debug($"I dropped on tick {GameTiming.CurTick} with seed {seed}!");
            return;
        }

        Log.Debug($"I run on tick {GameTiming.CurTick} with seed {seed} and chance {entity.Comp.FailChance}!");

        _actions.PerformAction(args.Entity.Owner, actionEntity);
        _actions.SetCooldown(actionEntity!, entity.Comp.JetPackFailureCooldown);
        _popup.PopupCursor(Loc.GetString(entity.Comp.JetPackFailurePopup), args.Entity.Owner, PopupType.LargeCaution);
    }
}
