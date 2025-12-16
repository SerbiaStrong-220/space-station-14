// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.SS220.Experience.Skill.Components;
using Content.Shared.SS220.Experience.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.Experience.Skill.Systems;

public sealed class DisarmOnDamageSkillSystem : SkillEntitySystem
{
    [Dependency] private readonly ExperienceSystem _experience = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    private readonly ProtoId<SkillTreePrototype> _affectedSkillTree = "PhysicalTraining";
    private readonly FixedPoint4 _damageToExperience = 150;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeEventToSkillEntity<DisarmOnDamageSkillComponent, DamageChangedEvent>(OnDamageChangedEvent);
    }

    public void OnDamageChangedEvent(Entity<DisarmOnDamageSkillComponent> entity, ref DamageChangedEvent args)
    {
        if (args.DamageDelta is null)
            return;

        if (DamageSpecifier.GetPositive(args.DamageDelta).GetTotal() < entity.Comp.DamageThreshold)
            return;

        if (!ResolveExperienceEntityFromSkillEntity(entity, out var experienceEntity))
            return;

        TryChangeStudyingProgress(entity, _affectedSkillTree, DamageSpecifier.GetPositive(args.DamageDelta).GetTotal() / _damageToExperience);

        if (!GetPredictedRandom(new() { GetNetEntity(entity).Id, args.DamageDelta.GetTotal().Int() }).Prob(entity.Comp.DisarmChance))
            return;

        if (_hands.EnumerateHeld(experienceEntity.Value.Owner).Count() == 0)
            return;

        foreach (var hand in _hands.EnumerateHands(experienceEntity.Value.Owner))
        {
            _hands.TryDrop(experienceEntity.Value.Owner, hand);
        }

        _popupSystem.PopupEntity(Loc.GetString(entity.Comp.OnDropPopup, ("target", Identity.Entity(experienceEntity.Value.Owner, EntityManager))), experienceEntity.Value.Owner, PopupType.MediumCaution);

        TryAddToAdminLogs(entity, $"dropping all items in hands due to {nameof(DisarmOnDamageSkillComponent)}", LogImpact.High);
    }
}
