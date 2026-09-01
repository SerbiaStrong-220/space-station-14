// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.Whitelist;

public sealed partial class EntityWhitelistSystem
{
    public bool CheckBothPrototype(EntityPrototype proto, EntityWhitelist? blacklist = null, EntityWhitelist? whitelist = null)
    {
        if (blacklist != null && IsValidPrototype(blacklist, proto))
            return false;

        return whitelist == null || IsValidPrototype(whitelist, proto);
    }

    private bool IsValidPrototype(EntityWhitelist list, EntityPrototype proto)
    {
        if (list.Components == null)
            return list.RequireAll;

        foreach (var compName in list.Components)
        {
            var present = proto.Components.ContainsKey(compName);
            if (present && !list.RequireAll)
                return true;
            if (!present && list.RequireAll)
                return false;
        }

        return list.RequireAll;
    }
}

