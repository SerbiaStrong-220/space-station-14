using Content.Shared.Damage;
using Content.Shared.Whitelist;

namespace Content.Shared.SS220.Trap;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed partial class TrapComponent : Component
{
    [DataField]
    public string FixtureId = "fix";

    /// <summary>
    /// 0 bla bla
    /// </summary>
    [DataField]
    public TimeSpan DurationStun = TimeSpan.Zero;

    [DataField]
    public EntityWhitelist Blacklist = new();

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public DamageSpecifier DamageOnTrapped;

    public bool IsSlammed = false;
}
