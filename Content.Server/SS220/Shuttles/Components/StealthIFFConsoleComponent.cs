using Content.Server.SS220.Shuttles.Systems;
using Content.Shared.Shuttles.Components;

namespace Content.Server.SS220.Shuttles.Components;

[RegisterComponent, Access(typeof(StealthIFFConsoleSystem))]
public sealed partial class StealthIFFConsoleComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite), DataField("allowedFlags")]
    public IFFFlags AllowedFlags = IFFFlags.HideLabel;

    [DataField]
    public TimeSpan StealthTime = TimeSpan.Zero;

    [DataField]
    public TimeSpan StealthCooldown = TimeSpan.Zero;

    [DataField]
    public bool HideOnInit = false;

    public TimeSpan CooldownUntil = TimeSpan.Zero;

    public TimeSpan StealthUntil = TimeSpan.Zero;
}
