// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Audio;

namespace Content.Server.SS220.Detective.Camera;

[RegisterComponent]
[Access(typeof(DetectiveCameraSystem))]
public sealed partial class DetectiveCameraComponent : Component
{
    [DataField("enabled"), ViewVariables(VVAccess.ReadWrite)]
    public bool Enabled;

    [DataField("activateCameraOnEnable"), ViewVariables(VVAccess.ReadWrite)]
    public bool ActivateCameraOnEnable;

    [DataField("powerOnSound"), ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier PowerOnSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_success.ogg");

    [DataField("powerOffSound"), ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier PowerOffSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_failed.ogg");
}
