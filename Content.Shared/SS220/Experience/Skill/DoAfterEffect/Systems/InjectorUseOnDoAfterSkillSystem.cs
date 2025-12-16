// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.SS220.ChangeSpeedDoAfters.Events;
using Content.Shared.SS220.Experience.DoAfterEffect.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.Experience.DoAfterEffect.Systems;

public sealed class InjectorUseOnDoAfterSkillSystem : BaseDoAfterSkillSystem<InjectorUseOnDoAfterSkillComponent, InjectorDoAfterEvent>
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    protected override void OnDoAfterEnd(Entity<InjectorUseOnDoAfterSkillComponent> entity, ref BeforeDoAfterCompleteEvent args)
    {
        if (args.Args.Target is null)
            return;

        if (!GetPredictedRandom(new() { GetNetEntity(entity).Id, GetNetEntity(args.Args.User).Id }).Prob(entity.Comp.FailureChance))
            return;

        if (ResolveExperienceEntityFromSkillEntity(entity.Owner, out var experienceEntity))
            return;

        // TOOD: imagine having good API
        if (_damageable.TryChangeDamage(args.Args.Target, entity.Comp.DamageOnFailure, origin: experienceEntity) is null)
            return;

        if (entity.Comp.FailurePopup is not null)
            _popup.PopupPredicted(Loc.GetString(entity.Comp.FailurePopup, ("target", Identity.Name(args.Args.Target.Value, EntityManager))), args.Args.Target.Value, args.Args.User, PopupType.MediumCaution);
    }
}
