using Robust.Shared.Prototypes;

namespace Content.Server.SS220.DarkForces.Ratvar.Righteous.Abilities;

[RegisterComponent]
public sealed partial class RatvarAbilitiesComponent : Component
{
    [DataField]
    public EntProtoId ActionClockMagic = "RatvarClockMagic";

    [DataField]
    public EntityUid? ActionClockMagicEntity;

    [DataField]
    public EntProtoId ActionMidasTouch = "RatvarMidasTouch";

    [DataField]
    public EntityUid? ActionMidasTouchEntity;
}
