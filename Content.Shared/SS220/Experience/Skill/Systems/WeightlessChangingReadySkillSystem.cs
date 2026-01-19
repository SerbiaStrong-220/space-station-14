// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Gravity;
using Content.Shared.Inventory;
using Content.Shared.Movement.Systems;
using Content.Shared.SS220.Experience.Skill.Components;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Robust.Shared.Random;

namespace Content.Shared.SS220.Experience.Skill.Systems;

public sealed class WeightlessChangingReadySkillSystem : SkillEntitySystem
{
    [Dependency] private readonly SharedJetpackSystem _jetpack = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    private const string HardSuitInventorySlot = "outerClothing";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeEventToSkillEntity<WeightlessChangingReadySkillComponent, WeightlessnessChangedEvent>(OnWeightlessChanged);
        SubscribeEventToSkillEntity<WeightlessChangingReadySkillComponent, RefreshWeightlessModifiersEvent>(OnRefreshWeightlessModifiers);
    }

    private void OnWeightlessChanged(Entity<WeightlessChangingReadySkillComponent> entity, ref WeightlessnessChangedEvent args)
    {
        if (!entity.Comp.Initialized)
            return;

        if (args.Weightless)
            return;

        if (!ResolveExperienceEntityFromSkillEntity(entity, out var experienceEntity))
            return;

        var chance = _inventory.TryGetSlotEntity(experienceEntity.Value.Owner, HardSuitInventorySlot, out var outerClothingEntity)
                        && _tag.HasAnyTag(outerClothingEntity.Value, entity.Comp.HardsuitTags)
                        ? entity.Comp.HardsuitFallChance
                        : entity.Comp.WithoutHardsuitFallChance;

        var predictedRandom = GetPredictedRandom(new() { GetNetEntity(entity).Id });

        if (!predictedRandom.Prob(chance))
            return;

        _stun.TryAddParalyzeDuration(experienceEntity.Value.Owner, entity.Comp.KnockdownDuration);
    }

    private void OnRefreshWeightlessModifiers(Entity<WeightlessChangingReadySkillComponent> entity, ref RefreshWeightlessModifiersEvent args)
    {
        if (!ResolveExperienceEntityFromSkillEntity(entity, out var experienceEntity))
            return;

        if (_jetpack.IsUserFlying(experienceEntity.Value.Owner))
            return;

        args.WeightlessAcceleration = entity.Comp.WeightlessAcceleration;
        args.WeightlessModifier = entity.Comp.WeightlessModifier;
        args.WeightlessFriction = entity.Comp.WeightlessFriction;
    }
}
