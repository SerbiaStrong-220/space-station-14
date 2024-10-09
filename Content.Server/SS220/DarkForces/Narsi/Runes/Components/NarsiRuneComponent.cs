using Content.Shared.SS220.DarkForces.Runes;

namespace Content.Server.SS220.DarkForces.Narsi.Runes.Components;

[RegisterComponent]
public sealed partial class NarsiRuneComponent : SharedNarsiRuneComponent
{
    [DataField("runeState")]
    [ViewVariables(VVAccess.ReadWrite)]
    public NarsiRuneState RuneState = NarsiRuneState.Idle;
}

public enum NarsiRuneState
{
    Idle,
    InUse
}
