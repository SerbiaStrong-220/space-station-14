// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
namespace Content.Server.SS220.SuperMatterCrystal.Components;

[RegisterComponent]
public sealed partial class SuperMatterSpecificConsumable : Component
{
    [DataField]
    public float AdditionalEnergyOnConsumption = 0f;
}
