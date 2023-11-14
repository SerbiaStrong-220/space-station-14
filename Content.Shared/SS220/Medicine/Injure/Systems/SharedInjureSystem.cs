// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Medicine.Injure.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Medicine.Injure.Systems;
public sealed partial class SharedInjureSystem : EntitySystem
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
    /// <param name="injureId"> Injure prototype's name </param>
    /// <returns> Injure EntityUid </returns>
    public EntityUid AddInjure(EntityUid target, InjuredComponent component, string injureId)
    {
        var injureEnt = Spawn(injureId);
        var injureComp = Comp<InjureComponent>(injureEnt);
        _transform.SetParent(injureEnt, target);

        if (injureComp.IsInnerWound)
            component.InnerInjures.Add(injureEnt);
        else
            component.OutterInjures.Add(injureEnt);
        var ev = new InjureAddedEvent(injureEnt, injureComp, component);
        RaiseLocalEvent(target, ref ev);
        return injureEnt;
    }

    public void RemoveInjure(EntityUid injure, EntityUid user, InjuredComponent component)
    {
        var injureComp = Comp<InjureComponent>(injure);
        if (injureComp.IsInnerWound)
            component.InnerInjures.Remove(injure);
        else
            component.OutterInjures.Remove(injure);
        var ev = new InjureRemovedEvent(injure, injureComp, component);
        RaiseLocalEvent(user, ref ev);
    }
}