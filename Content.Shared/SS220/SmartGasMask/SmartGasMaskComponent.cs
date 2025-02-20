// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Actions;
using Robust.Shared.Audio;

namespace Content.Shared.SS220.SmartGasMask;

/// <summary>
/// This is used for automatic notifications, selects via radial menu
/// </summary>
[RegisterComponent]
public sealed partial class SmartGasMaskComponent : Component
{
    [DataField]
    public List<string> SelectablePrototypes = [];

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier HaltSound = new SoundPathSpecifier("/Audio/SS220/Items/SmartGasMask/sound_voice_complionator_halt.ogg");

    [DataField]
    public LocId SuppMes = "smartgasmask-support-message";

    [DataField]
    public TimeSpan CdTimeHalt = TimeSpan.FromSeconds(10); //prevents spam

    [DataField]
    public TimeSpan CdTimeSupp = TimeSpan.FromSeconds(60); //prevents spam and adds a little balance

    public bool OnCdHalt = false;

    public bool OnCdSupp = false;
}

public sealed partial class SmartGasMaskOpenEvent : InstantActionEvent;
