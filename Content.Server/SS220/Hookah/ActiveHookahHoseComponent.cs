using System;

namespace Content.Server.SS220.Hookah;

[RegisterComponent]
public sealed partial class ActiveHookahHoseComponent : Component
{
    public TimeSpan Accum;
}
