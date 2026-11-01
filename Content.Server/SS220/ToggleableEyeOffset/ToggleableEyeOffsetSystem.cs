// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.
using Content.Shared.SS220.ToggleableEyeOffset;

namespace Content.Server.SS220.ToggleableEyeOffset.Systems;

public sealed partial class ToggleableEyeOffsetSystem : EntitySystem
{
    [Dependency] private SharedEyeSystem _eye = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ToggleableEyeOffsetComponent, ComponentStartup>(OnCompInit);
        SubscribeLocalEvent<ToggleableEyeOffsetComponent, ComponentShutdown>(OnCompShutdoown);
    }

    private void OnCompInit(Entity<ToggleableEyeOffsetComponent> ent, ref ComponentStartup args)
    {
        if (TryComp<EyeComponent>(ent.Owner, out var eyeComp))
        {
            _eye.SetPvsScale(ent.Owner, eyeComp.PvsScale + ent.Comp.PvsIncrease);
            Dirty(ent.Owner, eyeComp);
        }
    }

    private void OnCompShutdoown(Entity<ToggleableEyeOffsetComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<EyeComponent>(ent.Owner, out var eyeComp))
        {
            _eye.SetPvsScale(ent.Owner, eyeComp.PvsScale - ent.Comp.PvsIncrease);
            Dirty(ent.Owner, eyeComp);
        }
    }

}
