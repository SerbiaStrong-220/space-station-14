// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chemistry.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.SS220.ChangeSpeedDoAfters.Events;
using Content.Shared.SS220.Experience.DoAfterEffect.Components;
using Robust.Shared.Random;

namespace Content.Shared.SS220.Experience.DoAfterEffect.Systems;

public sealed class InjectorUseOnDoAfterSkillSystem : BaseDoAfterSkillSystem<InjectorUseOnDoAfterSkillComponent, InjectorDoAfterEvent>
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    protected override void OnDoAfterEnd(Entity<InjectorUseOnDoAfterSkillComponent> entity, ref BeforeDoAfterCompleteEvent args)
    {
        if (args.Args.Target is not { } target)
            return;

        if (!GetPredictedRandomOnCurTick(GetNetEntity(entity), GetNetEntity(args.Args.User)).Prob(entity.Comp.FailureChance))
            return;

        if (ResolveExperienceEntityFromSkillEntity(entity.Owner, out var experienceEntity))
            return;

        // TOOD: imagine having good API
        if (!_damageable.TryChangeDamage(target, entity.Comp.DamageOnFailure, origin: experienceEntity))
            return;

        if (entity.Comp.FailurePopup is not null)
            _popup.PopupPredicted(Loc.GetString(entity.Comp.FailurePopup, ("target", Identity.Name(target, EntityManager))), target, args.Args.User, PopupType.MediumCaution);
    }
}
