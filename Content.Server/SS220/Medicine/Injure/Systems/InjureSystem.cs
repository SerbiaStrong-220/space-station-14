
using Content.Shared.SS220.Medicine.Injure.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Medicine.Injure.Systems;

public sealed partial class InjureSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    public override void Initialize()
    {
        base.Initialize();
    }
    public EntityUid TryMakeInjure(EntityUid target, InjuredComponent component, string injureId)
    {
        var injureEnt = Spawn(injureId);
        _transform.SetParent(injureEnt, target);

        if (Comp<InjureComponent>(injureEnt).IsInnerWound)
            component.InnerInjures.Add(injureEnt);
        component.OutterInjures.Add(injureEnt);
        return injureEnt;
    }

}