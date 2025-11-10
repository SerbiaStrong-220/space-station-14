using Content.Server.Shuttles.Systems;
using Content.Shared.Shuttles.Components;

namespace Content.Server.Shuttles.Components;

[RegisterComponent, Access(typeof(ShuttleSystem))]
public sealed partial class IFFConsoleComponent : Component
{
    /// <summary>
    /// Flags that this console is allowed to set.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("allowedFlags")]
    public IFFFlags AllowedFlags = IFFFlags.HideLabel;
    // ss220 spacewar begin
    [DataField]
    public TimeSpan StealthTime = TimeSpan.Zero;

    [DataField]
    public TimeSpan StealthCooldown = TimeSpan.Zero;

    public TimeSpan CooldownUntil = TimeSpan.Zero;

    public TimeSpan StealthUntil = TimeSpan.Zero;
    // ss220 spacewar end
}
