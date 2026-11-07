using Content.Shared.SS220.DarkForces.Ratvar.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.DarkForces.Ratvar.Righteous.Abilities.Midas;

[RegisterComponent]
public sealed partial class MidasMaterialComponent : Component
{
    [DataField]
    public HashSet<ProtoId<RatvarMidasTouchablePrototype>> Targets = new();
}