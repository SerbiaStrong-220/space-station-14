using Content.Shared.Materials;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.SS220.DarkForces.Ratvar.Righteous.Structures.Workshop;

[RegisterComponent]
public sealed partial class RatvarworkShopComponent : Component
{
    [DataField]
    public bool InProgress;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<MaterialPrototype>))]
    public string RequiredMaterial = "BrassPlasteel";
}
