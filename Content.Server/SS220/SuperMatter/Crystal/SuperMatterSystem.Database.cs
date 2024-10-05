// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Linq;
using Content.Server.SS220.SuperMatter;
using Content.Server.SS220.SuperMatterCrystal.Components;
using Content.Shared.Atmos;
using Content.Shared.CartridgeLoader;
using Content.Shared.SS220.CCVars;
using Content.Shared.SS220.SuperMatter.Ui;
using Robust.Shared.Configuration;

namespace Content.Server.SS220.SuperMatterCrystal;
// TODO: cache here data about SM in dictionary of uid
// TODO: handle client request for information like spamming of console etc etc
// TODO: added: Fun!
public sealed partial class SuperMatterSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;

    private float _broadcastDelay;

    // TODO add shifts counter for not delaminate
    private void InitializeDatabase()
    {
        Subs.CVar(_config, CCVars220.SuperMatterUpdateNetworkDelay, OnBroadcastDelayChanged, true);
    }
    public void BroadcastData(Entity<SuperMatterComponent> crystal)
    {
        var (uid, comp) = crystal;

        var matterDerv = comp.MatterDervAccumulator / comp.UpdatesBetweenBroadcast;
        var internalEnergyDerv = comp.InternalEnergyDervAccumulator / comp.UpdatesBetweenBroadcast;
        var pressure = comp.PressureAccumulator / comp.UpdatesBetweenBroadcast;
        comp.Name ??= MetaData(crystal.Owner).EntityName;
        Dictionary<Gas, float> gasRatios = new();
        foreach (var gas in Enum.GetValues<Gas>())
        {
            gasRatios.Add(gas, comp.AccumulatedGasesMoles[gas] / comp.AccumulatedGasesMoles.Values.Sum());
        }
        var totalMoles = comp.AccumulatedGasesMoles.Values.Sum() / comp.UpdatesBetweenBroadcast;
        // just in case...
        if (!HasComp<TransformComponent>(uid))
        {
            Log.Error($" Tried to get TransformComp of {EntityManager.ToPrettyString(crystal)}, but it hasnt it");
            return;
        }

        var ev = new SuperMatterStateUpdate(uid.Id, EntityManager.GetNetEntity(Transform(uid).GridUid), comp.Activated,
                                            comp.Name, GetIntegrity(comp), pressure, comp.Temperature,
                                            (comp.Matter, matterDerv),
                                            (comp.InternalEnergy, internalEnergyDerv),
                                            gasRatios, totalMoles,
                                            (comp.IsDelaminate, comp.TimeOfDelamination));
        RaiseNetworkEvent(ev);

        comp.MatterDervAccumulator = 0;
        comp.InternalEnergyDervAccumulator = 0;
        comp.UpdatesBetweenBroadcast = 0;
        comp.PressureAccumulator = 0;
        ZeroGasMolesAccumulator(comp);
    }

    private void AddGasesToAccumulator(SuperMatterComponent smComp, GasMixture gasMixture)
    {
        foreach (var gas in Enum.GetValues<Gas>())
        {
            smComp.AccumulatedGasesMoles[gas] += gasMixture.GetMoles((int)gas);
        }
    }
    private void InitGasMolesAccumulator(SuperMatterComponent smComp)
    {
        foreach (var gas in Enum.GetValues<Gas>())
        {
            smComp.AccumulatedGasesMoles.Add(gas, 0f);
        }
    }
    private void ZeroGasMolesAccumulator(SuperMatterComponent smComp)
    {
        foreach (var gas in Enum.GetValues<Gas>())
        {
            smComp.AccumulatedGasesMoles[gas] = 0;
        }
    }
    private void OnBroadcastDelayChanged(float delay)
    {
        _broadcastDelay = delay;
    }
}
