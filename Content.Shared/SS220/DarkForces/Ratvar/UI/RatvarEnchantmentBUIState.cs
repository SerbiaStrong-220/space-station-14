using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.DarkForces.Ratvar.UI;

[Serializable, NetSerializable]
public sealed class RatvarEnchantmentBUIState : BoundUserInterfaceState
{
    public List<EnchantmentUIModel> Models { get; set; }

    public RatvarEnchantmentBUIState(List<EnchantmentUIModel> models)
    {
        Models = models;
    }
}

[Serializable, NetSerializable]
public record EnchantmentUIModel(string Id, string Name, string Visuals, SpriteSpecifier Icon);

[Serializable, NetSerializable]
public sealed class RatvarEnchantmentSelectedMessage : BoundUserInterfaceMessage
{
    public string Id { get; private set; }
    public string Visuals { get; private set; }

    public RatvarEnchantmentSelectedMessage(string id, string visuals)
    {
        Id = id;
        Visuals = visuals;
    }
}

[Serializable, NetSerializable]
public enum RatvarEnchantmentUIKey
{
    Key
}
