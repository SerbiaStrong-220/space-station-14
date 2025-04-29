using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Stunprod;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class StunprodComponent : Component
{
    [DataField]
    public float CurrentCharge;

    [DataField(required:true)]
    public float EnergyPerUse;
}
