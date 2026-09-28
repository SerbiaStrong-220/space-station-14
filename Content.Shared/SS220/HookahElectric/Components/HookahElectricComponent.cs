using System.Numerics;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.HookahElectric.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HookahElectricComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? LeftHose;

    [DataField, AutoNetworkedField]
    public EntityUid? RightHose;

    [DataField]
    public Vector2 LeftHoseOffset = new(-0.15f, 0.1f);

    [DataField]
    public Vector2 RightHoseOffset = new(0.15f, 0.1f);

    [DataField]
    public SoundSpecifier ToggleOnSound =
        new SoundPathSpecifier("/Audio/Machines/button.ogg");

    [DataField]
    public SoundSpecifier ToggleOffSound =
        new SoundPathSpecifier("/Audio/Machines/button.ogg");

}
