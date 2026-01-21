// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Clothing;
using Content.Shared.Gravity;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.SS220.Experience.Skill.Components;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Robust.Shared.Random;

namespace Content.Shared.SS220.Experience.Skill.Systems;

public sealed class WeightlessChangingReadySkillSystem : SkillEntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedJetpackSystem _jetpack = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private const string HardSuitInventorySlot = "outerClothing";

    private const uint SpawnTickBorder = 20;

    private readonly LocId _evadedFallPopup = "weightless-changing-ready-skill-evaded-fall-popup";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeEventToSkillEntity<WeightlessChangingReadySkillComponent, WeightlessnessChangedEvent>(OnWeightlessChanged);
        SubscribeEventToSkillEntity<WeightlessChangingReadySkillComponent, MagbootsUpdateStateEvent>(OnMagbootsUpdateState);

        SubscribeEventToSkillEntity<WeightlessChangingReadySkillComponent, RefreshWeightlessModifiersEvent>(OnRefreshWeightlessModifiers, before: [typeof(SharedJetpackSystem)]);
    }

    private void OnWeightlessChanged(Entity<WeightlessChangingReadySkillComponent> entity, ref WeightlessnessChangedEvent args)
    {
        // it used to prevent annoying falling when gravity engine fails to enable at time
        if (GameTiming.CurTick.Value < entity.Comp.CreationTick.Value + SpawnTickBorder)
            return;

        // Okay this is how I deal with prediction resetting
        // If you skip this check you will add a KnockedDownComponent during resetting procedure
        if (GameTiming.ApplyingState)
            return;

        if (!ResolveExperienceEntityFromSkillEntity(entity, out var experienceEntity))
            return;

        if (args.Weightless || entity.Comp.MagbootsActive)
            return;

        var chance = _inventory.TryGetSlotEntity(experienceEntity.Value.Owner, HardSuitInventorySlot, out var outerClothingEntity)
                        && _tag.HasAnyTag(outerClothingEntity.Value, entity.Comp.HardsuitTags)
                        ? entity.Comp.HardsuitFallChance
                        : entity.Comp.WithoutHardsuitFallChance;

        var predictedRandom = GetPredictedRandomOnCurTick(new() { GetNetEntity(entity).Id });

        if (!predictedRandom.Prob(chance))
        {
            _popup.PopupPredicted(Loc.GetString(_evadedFallPopup, ("entity", Identity.Name(experienceEntity.Value, EntityManager))), experienceEntity.Value.Owner, experienceEntity);
            return;
        }

        // TODO: if you make drop: true this will lead to engine error:
        // ex: `Grid traversal attempted to handle movement of джетпак (5289/n5289, JetpackBlueFilled) while moving name-name (5172/n5172, MobReptilian, ckey)`
        _stun.TryKnockdown(experienceEntity.Value.Owner, entity.Comp.KnockdownDuration, drop: false);
    }

    private void OnMagbootsUpdateState(Entity<WeightlessChangingReadySkillComponent> entity, ref MagbootsUpdateStateEvent args)
    {
        entity.Comp.MagbootsActive = args.State;
    }

    private void OnRefreshWeightlessModifiers(Entity<WeightlessChangingReadySkillComponent> entity, ref RefreshWeightlessModifiersEvent args)
    {
        if (!ResolveExperienceEntityFromSkillEntity(entity, out var experienceEntity))
            return;

        if (_jetpack.IsUserFlying(experienceEntity.Value.Owner))
            return;

        args.WeightlessAcceleration *= entity.Comp.WeightlessAcceleration;
        args.WeightlessModifier *= entity.Comp.WeightlessModifier;
        args.WeightlessFriction *= entity.Comp.WeightlessFriction;
    }
}
