// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Nutrition.EntitySystems;

namespace Content.Server.SS220.Nutrition;

/// <summary>
/// This component makes food more desirable for a NPC mobs like mice.
/// </summary>
[RegisterComponent, Access(typeof(IngestionSystem))]
public sealed partial class DesiredFoodComponent : Component
{
    [DataField]
    public float DesireLevel = 1f;
}
