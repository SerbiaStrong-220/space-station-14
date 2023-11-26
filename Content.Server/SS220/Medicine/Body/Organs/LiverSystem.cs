
using Content.Server.SS220.Medicine.Injury.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.SS220.Medicine.Injury.Components;
using Content.Shared.SS220.Medicine.Injury.Systems;

namespace Content.Server.SS220.Medicine.Body.Organs;


public sealed partial class LiverSystem : EntitySystem
{
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedInjurySystem _injury = default!;
    public override void Initialize()
    {
        base.Initialize();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
    }
}