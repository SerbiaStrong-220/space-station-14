using System;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Hookah.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class HookahHoseComponent : Component
{
    public EntityUid HookahUid;

    [DataField]
    public float MaxDistance = 3f;

    [DataField]
    public TimeSpan CheckInterval = TimeSpan.FromSeconds(0.25);
}
