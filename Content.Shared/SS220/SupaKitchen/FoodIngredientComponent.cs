// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.SupaKitchen;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FoodIngredientComponent : Component
{
    public const string StorageId = "topping_storage";

    [DataField, ViewVariables, AutoNetworkedField]
    public ToppingFilter ToppingFilter = new();

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool CanAddIngredients = false;

    public Container IngredientContainer = default!;
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class ToppingFilter
{
    [DataField, ViewVariables]
    public HashSet<EntProtoId>? IdWhitelist;
}
