using Content.Shared.SS220.Discord;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Shlepovend;

public abstract class SharedShlepovendSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    public ShlepaRewardGroupPrototype? GetHighestTier(SponsorTier[] tiers)
    {
        // Get highest player's tier
        var highestAvailableTierVal = 0;
        SponsorTier? highestAvailableTier = null;
        foreach (var tier in tiers)
        {
            if ((int) tier > highestAvailableTierVal)
            {
                highestAvailableTierVal = (int) tier;
                highestAvailableTier = tier;
            }
        }

        if (highestAvailableTier == null)
            return null;

        if (!_prototype.TryGetInstances<ShlepaRewardGroupPrototype>(out var protos))
            return null;

        // Try to get a role proto with a corresponding enum
        foreach (var (_, proto) in protos)
        {
            if (proto.RequiredRole != null && proto.RequiredRole == highestAvailableTier.Value)
                return proto;
        }

        return null;
    }

    public bool RoleExists(SponsorTier role)
    {
        if (!_prototype.TryGetInstances<ShlepaRewardGroupPrototype>(out var protos))
            return false;

        // Try to get a role proto with a corresponding enum
        foreach (var (_, proto) in protos)
        {
            if (proto.RequiredRole != null && proto.RequiredRole == role)
                return true;
        }

        return false;
    }
}

[Serializable, NetSerializable]
public sealed class ShlepovendTokenAmountMsg : EntityEventArgs
{
    public int Tokens = 0;
}

[Serializable, NetSerializable]
public sealed class ShlepovendPurchaseMsg : BoundUserInterfaceMessage
{
    public ProtoId<ShlepaRewardGroupPrototype> GroupId;
    public EntProtoId ItemId;
}
