// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.SS220.Experience.Skill.Components;
using Content.Shared.SS220.Experience.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.Experience.Skill.Systems;

public sealed class DisarmOnDamageSkillSystem : EntitySystem
{
    [Dependency] private readonly ExperienceSystem _experience = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        _experience.RelayEventToSkillEntity<DisarmOnDamageSkillComponent, DamageChangedEvent>();

        SubscribeLocalEvent<DisarmOnDamageSkillComponent, DamageChangedEvent>(OnDamageChangedEvent);
    }

    public void OnDamageChangedEvent(Entity<DisarmOnDamageSkillComponent> entity, ref DamageChangedEvent args)
    {
        if (args.DamageDelta is null)
            return;

        if (DamageSpecifier.GetPositive(args.DamageDelta).GetTotal() < entity.Comp.DamageThreshold)
            return;

        // TODO: Once we have predicted randomness delete this for something sane...
        var seed = SharedRandomExtensions.HashCodeCombine(new() { (int)_gameTiming.CurTick.Value, GetNetEntity(entity).Id, args.DamageDelta.GetTotal().Int() });
        var rand = new System.Random(seed);

        if (!rand.Prob(entity.Comp.DisarmChance))
            return;

        if (!_experience.TryGetExperienceEntityFromSkillEntity(entity.Owner, out var experienceEntity))
            return;

        if (_hands.EnumerateHeld(experienceEntity.Value.Owner).Count() == 0)
            return;

        foreach (var hand in _hands.EnumerateHands(experienceEntity.Value.Owner))
        {
            _hands.TryDrop(experienceEntity.Value.Owner, hand);
        }

        _popupSystem.PopupEntity(Loc.GetString(entity.Comp.OnDropPopup, ("target", Identity.Entity(experienceEntity.Value.Owner, EntityManager))), experienceEntity.Value.Owner, PopupType.MediumCaution);

        _experience.AddToAdminLogs(entity, "dropped all items in hands", LogImpact.High);
    }
}
