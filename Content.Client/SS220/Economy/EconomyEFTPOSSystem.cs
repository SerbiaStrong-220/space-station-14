// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Economy;

namespace Content.Client.SS220.Economy;

public sealed class EconomyEFTPOSSystem : SharedEconomyEFTPOSSystem
{
    protected override void OnEnterButtonPressed(Entity<EconomyEFTPOSComponent> ent, ref EconomyEFTPOSKeypadEnterMessage args)
    {
        // Client
    }

    protected override string GetOwner(int bankAccountId)
    {
        return string.Empty;
    }

}
