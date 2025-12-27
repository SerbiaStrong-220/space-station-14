using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Weapons.Melee.UseDelayBlockAtack;

[RegisterComponent, NetworkedComponent]
public sealed partial class UseDelayBlockMeleeAttackComponent : Component
{
    [DataField]
    public List<string> Delays = new(){"default"};
}
