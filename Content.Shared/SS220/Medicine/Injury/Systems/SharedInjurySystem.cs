// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Medicine.Injury;
using Content.Shared.SS220.Medicine.Injury.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Medicine.Injury.Systems;
public sealed partial class SharedInjurySystem : EntitySystem
{

    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    public override void Initialize()
    {
        base.Initialize();
    }

    public List<EntityUid>? GetEntityInjuries(EntityUid uid, InjuriesContainerComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return null;
        return component.Injuries;
    }

    /// <summary>
    /// Add injure to target
    /// </summary>
    /// <param name="target"></param>
    /// <param name="component"></param>
    /// <param name="injuryId"> Injure prototype's name </param>
    /// <returns> Injure EntityUid </returns>
    public EntityUid AddInjure(EntityUid target, InjuriesContainerComponent component, InjurySeverityStages severity, string injuryId)
    {
        var injuryEnt = Spawn(injuryId);
        var injuryComp = Comp<InjuryComponent>(injuryEnt);
        injuryComp.Severity = severity;
        _transform.SetParent(injuryEnt, target);

        component.Injuries.Add(injuryEnt);

        var ev = new InjuryAddedEvent(injuryEnt, injuryComp, component);
        RaiseLocalEvent(target, ref ev);
        return injuryEnt;
    }

    public void RemoveInjury(EntityUid injury, EntityUid user, InjuriesContainerComponent component)
    {
        var injuryComp = Comp<InjuryComponent>(injury);

        component.Injuries.Remove(injury);

        var ev = new InjuryRemovedEvent(injury, injuryComp, component);
        RaiseLocalEvent(user, ref ev);
    }

    public void IncreaseInjureSeverity(EntityUid injury, InjurySeverityStages newSeverity)
    {
        var injuryComp = Comp<InjuryComponent>(injury);

        var ev = new InjurySeverityStageChangedEvent(injury, injuryComp, injuryComp.Severity, newSeverity);

        injuryComp.Severity = newSeverity;

        RaiseLocalEvent(injury, ref ev);
    }
}