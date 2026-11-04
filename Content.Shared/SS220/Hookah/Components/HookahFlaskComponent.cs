using Robust.Shared.Audio;

namespace Content.Shared.SS220.Hookah.Components;

[RegisterComponent]
public sealed partial class HookahFlaskComponent : Component
{
    [DataField]
    public SoundSpecifier AssemblySound =
        new SoundPathSpecifier("/Audio/Items/welder.ogg");
}
