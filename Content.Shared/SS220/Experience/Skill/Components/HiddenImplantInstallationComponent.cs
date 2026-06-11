// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Surgery.Components;
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
