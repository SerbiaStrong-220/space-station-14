﻿using Robust.Shared.GameStates;

namespace Content.Shared.SecretStation.DarkForces.Narsi.Roles;

[RegisterComponent, NetworkedComponent]
public sealed partial class NarsiCultistComponent : Component
{
    [DataField]
    public Dictionary<string, EntityUid?> Abilities = new();
}
