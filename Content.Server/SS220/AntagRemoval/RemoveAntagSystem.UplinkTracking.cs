// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
namespace Content.Server.SS220.AntagRemoval;

public sealed partial class RemoveAntagSystem
{
    /// <summary>
    /// Records the exact uplink entities created by TraitorRuleSystem. The
    /// store can be bound to a PDA or to an implant; the same UID is revoked
    /// regardless of where the device is moved later.
    /// </summary>
    public void RecordGrantedTraitorUplink(
        EntityUid body,
        EntityUid store,
        EntityUid? implant)
    {
        var origin = EnsureComp<AntagRemovalGrantComponent>(body);
        if (!origin.Grants.TryGetValue(RemoveAntagRoles.Traitor, out var grant))
        {
            grant = new AntagGrantRecord { RoleId = RemoveAntagRoles.Traitor };
            origin.Grants.Add(RemoveAntagRoles.Traitor, grant);
        }

        grant.UplinkStores.Add(store);
        if (implant is { } entity)
            grant.UplinkImplants.Add(entity);
    }
}
