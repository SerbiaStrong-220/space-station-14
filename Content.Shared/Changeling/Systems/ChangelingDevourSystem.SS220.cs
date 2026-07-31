using Content.Shared.Changeling.Components;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Changeling.Systems;

public sealed partial class ChangelingDevourSystem
{
    /// <summary>
    /// Moves the configured devour ability to a new body without carrying transient action or sound entities.
    /// </summary>
    public bool TryTransferTo(Entity<ChangelingDevourComponent?> source, EntityUid target)
    {
        if (!Resolve(source, ref source.Comp, false) ||
            TerminatingOrDeleted(target) ||
            HasComp<ChangelingDevourComponent>(target))
        {
            return false;
        }

        AddComp(target, new ChangelingDevourComponent
        {
            ChangelingDevourAction = source.Comp.ChangelingDevourAction,
            Whitelist = source.Comp.Whitelist,
            ConsumeNoise = source.Comp.ConsumeNoise,
            DevourWindupNoise = source.Comp.DevourWindupNoise,
            DevourWindupTime = source.Comp.DevourWindupTime,
            DevourConsumeTime = source.Comp.DevourConsumeTime,
            WindupDamage = source.Comp.WindupDamage,
            DevourDamage = source.Comp.DevourDamage,
            ProtectiveDamageTypes = new List<ProtoId<DamageTypePrototype>>(source.Comp.ProtectiveDamageTypes),
            DevourPreventionPercentageThreshold = source.Comp.DevourPreventionPercentageThreshold,
        });
        RemComp<ChangelingDevourComponent>(source.Owner);
        return true;
    }
}
