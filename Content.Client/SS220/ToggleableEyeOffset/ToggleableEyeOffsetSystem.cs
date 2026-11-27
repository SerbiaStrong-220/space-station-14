// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.SS220.ToggleableEyeOffset;
using Content.Shared.SS220.Movement.Events;

namespace Content.Client.SS220.EyeOffsetInCombatMode;

public sealed partial class ToggleableEyeOffsetSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ToggleableEyeOffsetComponent, CanApplyEyeCursorOffsetEvent>(OnCanApplyOffset);
    }
    private void OnCanApplyOffset(Entity<ToggleableEyeOffsetComponent> ent, ref CanApplyEyeCursorOffsetEvent args)
    {
        if (TryComp<ToggleableEyeOffsetComponent>(ent.Owner, out var toggleOffsetComp))
        {
            if (!toggleOffsetComp.Online)
            {
                args.Cancelled = true;
                return;
            }

            if (TryComp<MobStateComponent>(ent.Owner, out var mobStateComp) && mobStateComp.CurrentState != MobState.Alive)
            {
                args.Cancelled = true;
                return;
            }
        }
    }
}
