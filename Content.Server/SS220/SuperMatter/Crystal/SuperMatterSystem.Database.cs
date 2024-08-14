// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.SS220.SuperMatterCrystal.Components;
using Content.Shared.SS220.SuperMatter.Ui;

namespace Content.Server.SS220.SuperMatterCrystal;
// TODO: cache here data about SM in dictionary of uid
// TODO: handle client request for information like spamming of console etc etc
// TODO: added: Fun!
public sealed partial class SuperMatterSystem : EntitySystem
{
    public void BroadcastData(Entity<SuperMatterComponent> crystal)
    {
        var (uid, comp) = crystal;

        var matterDerv = comp.MatterDervAccumulator / comp.UpdatesBetweenBroadcast;
        var internalEnergyDerv = comp.InternalEnergyDervAccumulator / comp.UpdatesBetweenBroadcast;
        var pressure = comp.PressureAccumulator / comp.UpdatesBetweenBroadcast;
        comp.Name ??= MetaData(crystal.Owner).EntityName;

        var ev = new SuperMatterStateUpdate(uid.Id, comp.Name, GetIntegrity(comp),
                                            pressure, comp.Temperature,
                                            (comp.Matter, matterDerv),
                                            (comp.InternalEnergy, internalEnergyDerv),
                                            (comp.IsDelaminate, comp.TimeOfDelamination));
        RaiseNetworkEvent(ev);

        comp.MatterDervAccumulator = 0;
        comp.InternalEnergyDervAccumulator = 0;
        comp.UpdatesBetweenBroadcast = 0;
    }
}
