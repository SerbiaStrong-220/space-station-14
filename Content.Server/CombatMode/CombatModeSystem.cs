using Content.Server.NPC.HTN;
using Content.Shared.CombatMode;
using Content.Shared.FCB.CombatMode;

namespace Content.Server.CombatMode;

public sealed class CombatModeSystem : SharedCombatModeSystem
{
    protected override bool IsNpc(EntityUid uid)
    {
        return HasComp<HTNComponent>(uid);
    }

    protected override void OnActionPerform(EntityUid uid, CombatModeComponent component, ToggleCombatActionEvent args)
    {
        base.OnActionPerform(uid, component, args);

        var ev = new OnCombatModeToggledEvent();
        RaiseLocalEvent(uid, ref ev);
    }
}
