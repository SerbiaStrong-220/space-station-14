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

    /// <summary>
    /// Add injure to target
    /// </summary>
    /// <param name="target"></param>
    /// <param name="component"></param>
    /// <param name="injuryId"> Injure prototype's name </param>
    /// <returns> Injure EntityUid </returns>
    public EntityUid AddInjure(EntityUid target, InjuriesContainerComponent component, InjuryStages severity, string injuryId)
    {
        var injuryEnt = Spawn(injuryId);
        var injuryComp = Comp<InjuryComponent>(injuryEnt);
        injuryComp.Severity = severity;
        _transform.SetParent(injuryEnt, target);

        if (injuryComp.Localization == InjuryLocalization.Inner)
            component.InnerInjuries.Add(injuryEnt);
        else
            component.OuterInjuries.Add(injuryEnt);
        var ev = new InjuryAddedEvent(injuryEnt, injuryComp, component);
        RaiseLocalEvent(target, ref ev);
        return injuryEnt;
    }

    public void RemoveInjury(EntityUid injury, EntityUid user, InjuriesContainerComponent component)
    {
        var injuryComp = Comp<InjuryComponent>(injury);
        if (injuryComp.Localization == InjuryLocalization.Inner)
            component.InnerInjuries.Remove(injury);
        else
            component.OuterInjuries.Remove(injury);
        var ev = new InjuryRemovedEvent(injury, injuryComp, component);
        RaiseLocalEvent(user, ref ev);
    }
}