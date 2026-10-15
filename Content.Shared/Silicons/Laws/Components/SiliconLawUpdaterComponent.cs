using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Silicons.Laws.Components;

/// <summary>
/// Whenever an entity is inserted with silicon laws it will update the relevant entity's laws.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SiliconLawUpdaterComponent : Component
{
    // SS220 random lawset begin
    /// <summary>
    /// Upload the station default and update its non-subverted, randomized silicons,
    /// instead of using the component selection below.
    /// </summary>
    [DataField]
    public bool UpdateStationLawset;
    // SS220 random lawset end

    /// <summary>
    /// Entities to update
    /// </summary>
    [DataField(required: true)]
    public ComponentRegistry Components;
}
