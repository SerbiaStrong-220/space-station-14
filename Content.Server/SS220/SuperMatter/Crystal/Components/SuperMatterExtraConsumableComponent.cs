// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
namespace Content.Server.SS220.SuperMatterCrystal.Components;

[RegisterComponent]
public sealed partial class SuperMatterExtraConsumableComponent : Component
{
    /// <summary>
    /// Before getting values raise local event <see cref="SyncSuperMatterBoltStats"/> otherwise it will have wrong value.
    /// </summary>
    [DataField]
    public float AdditionalMatterOnConsumption = 0f;
    /// <summary>
    /// <inheritdoc cref="AdditionalMatterOnConsumption"/>
    /// </summary>
    [DataField]
    public float AdditionalEnergyOnConsumption = 0f;
}
