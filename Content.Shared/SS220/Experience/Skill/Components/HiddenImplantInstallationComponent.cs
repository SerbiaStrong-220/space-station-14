// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Experience.Skill.Components;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HiddenImplantInstallationComponent : Component
{
    [DataField(required: true)]
    [AutoNetworkedField]
    public Dictionary<HiddenInstallLevel, float> InstallChances;


    [DataField(required: true)]
    [AutoNetworkedField]
    public float HiddenInstallChance;
}

/// <summary>
/// Should much minimal required level to see implant
/// Note having required level does not guarantee that the implant will be detected
/// </summary>
public enum HiddenInstallLevel : byte
{
    Easy = 2,
    Medium = 3,
    Hard = 4,
}
