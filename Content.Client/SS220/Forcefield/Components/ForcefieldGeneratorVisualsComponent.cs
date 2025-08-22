// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
namespace Content.Client.SS220.Forcefield.Components;

[RegisterComponent]
public sealed partial class ForcefieldGeneratorVisualsComponent : Component
{
    [DataField]
    public string PowerState = "power";

    [DataField]
    public int PowerSteps;
}
