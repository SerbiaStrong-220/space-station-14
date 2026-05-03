// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Shared.SS220.ComplexRepairable;

namespace Content.Server.SS220.EyeOffsetInCombatMode.Systems;

public sealed partial class EyeOffsetInCombatModeSystem : EntitySystem
{
    [Dependency] private readonly SharedEyeSystem _eye = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EyeOffsetInCombatModeComponent, ComponentStartup>(OnCompInit);
        SubscribeLocalEvent<EyeOffsetInCombatModeComponent, ComponentShutdown>(OnCompShutdoown);
    }

    private void OnCompInit(Entity<EyeOffsetInCombatModeComponent> ent, ref ComponentStartup args)
    {
        if (TryComp<EyeComponent>(ent.Owner, out var eyeComp))
        {
            _eye.SetPvsScale(ent.Owner, eyeComp.PvsScale + ent.Comp.PvsIncrease);
            Dirty(ent.Owner, eyeComp);
        }
    }

    private void OnCompShutdoown(Entity<EyeOffsetInCombatModeComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<EyeComponent>(ent.Owner, out var eyeComp))
        {
            _eye.SetPvsScale(ent.Owner, eyeComp.PvsScale - ent.Comp.PvsIncrease);
            Dirty(ent.Owner, eyeComp);
        }
    }

}
