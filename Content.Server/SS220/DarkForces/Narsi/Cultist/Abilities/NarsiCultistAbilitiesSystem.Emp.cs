using Content.Server.Emp;
using Content.Shared.SS220.DarkForces.Narsi.Abilities.Events;
using Content.Shared.SS220.DarkForces.Narsi.Roles;

namespace Content.Server.SS220.DarkForces.Narsi.Cultist.Abilities;

public sealed partial class NarsiCultistAbilitiesSystem
{
    [Dependency] private readonly EmpSystem _empSystem = default!;

    private void InitializeEmp()
    {
        SubscribeLocalEvent<NarsiCultistComponent, NarsiCultistEmpEvent>(OnEmpEvent);
    }

    private void OnEmpEvent(EntityUid uid, NarsiCultistComponent component, NarsiCultistEmpEvent args)
    {
        if (args.Handled)
            return;

        var transform = Transform(args.Performer);
        var level = _progressSystem.GetAbilityLevel(EmpAction);
        var range = level switch
        {
            1 => 3,
            2 => 6,
            _ => 9
        };
        var energyConsumption = level switch
        {
            1 => 20000,
            2 => 30000,
            _ => 40000
        };

        var duration = level switch
        {
            1 => 10,
            2 => 20,
            _ => 30
        };

        _empSystem.EmpPulse(
            coordinates: transform.MapPosition,
            range: range,
            energyConsumption: energyConsumption,
            duration: duration
        );
        OnCultistAbility(uid, args);
        args.Handled = true;
    }
}
