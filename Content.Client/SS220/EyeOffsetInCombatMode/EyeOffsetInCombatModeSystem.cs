// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.CombatMode;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.SS220.EyeOffsetInCombatMode;
using Content.Shared.SS220.Movement.Events;

namespace Content.Client.SS220.EyeOffsetInCombatMode;

public sealed partial class EyeOffsetInCombatModeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EyeOffsetInCombatModeComponent, CanApplyEyeCursorOffsetEvent>(OnCanApplyOffset);
    }
    private void OnCanApplyOffset(Entity<EyeOffsetInCombatModeComponent> ent, ref CanApplyEyeCursorOffsetEvent args)
    {
        if (TryComp<EyeOffsetInCombatModeComponent>(ent.Owner, out var combatOffsetComp))
        {
            if (!combatOffsetComp.Online)
            {
                args.Cancelled = true;
                return;
            }

            if (!TryComp<CombatModeComponent>(ent.Owner, out var combatModeComp) || !combatModeComp.IsInCombatMode)
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
