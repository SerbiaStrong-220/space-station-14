// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Audio;

namespace Content.Shared.SS220.Surgery.Components;

[RegisterComponent]
public sealed partial class SurgeryToolComponent : Component
{
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public SurgeryToolType ToolType = SurgeryToolType.invalid;

    [DataField("sound")]
    public SoundSpecifier? StartSurgerySound = null;
}

// for now I need only this ones
public enum SurgeryToolType
{
    invalid = 0,
    scalpel,
    retractor,
    hemostat,
    saw,
    cautery
}
