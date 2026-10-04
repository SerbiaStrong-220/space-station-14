using Content.Shared.CombatMode;

namespace Content.Server.CombatMode;

public sealed class CombatModeSystem : SharedCombatModeSystem
//SS220 weapon overhaul begin
{
    public override void SetInCombatMode(EntityUid entity, bool value, CombatModeComponent? component = null)
    {
        base.SetInCombatMode(entity, value, component);
    }
}
//SS220 weapon overhaul end
