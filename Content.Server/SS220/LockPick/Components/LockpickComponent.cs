using Robust.Shared.Audio;

namespace Content.Server.SS220.LockPick.Components;

[RegisterComponent]
public sealed partial class LockpickComponent : Component
{
    [DataField]
    public SoundSpecifier LockPickSound = new SoundPathSpecifier("/Audio/SS220/Effects/Drop/needle.ogg");

    public readonly float LockPickSpeedModifier = 1f;
}
