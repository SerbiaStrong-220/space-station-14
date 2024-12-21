// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Text;
using Content.Server.Popups;
using Content.Server.SS220.SuperMatter.Emitter;
using Content.Server.SS220.SuperMatterCrystal.Components;
using Content.Shared.Administration;
using Content.Shared.Singularity.Components;

namespace Content.Server.SS220.SuperMatterCrystal;

public sealed partial class SuperMatterSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public void ConsumeObject(EntityUid targetUid, Entity<SuperMatterComponent> crystal, bool spawnEntity = false)
    {
        if (HasComp<SuperMatterImmuneComponent>(targetUid))
            return;

        if (HasComp<EventHorizonComponent>(targetUid))
            return;

        if (HasComp<MetaDataComponent>(targetUid)
           && MetaData(targetUid).EntityDeleted)
            return;

        if (HasComp<AdminFrozenComponent>(crystal.Owner))
            return;

        var (crystalUid, smComp) = crystal;

        if (!smComp.Activated)
        {
            var ev = new SuperMatterActivationEvent(crystalUid, targetUid);
            RaiseLocalEvent(crystalUid, ev);
        }

        if (TryComp<SuperMatterExtraConsumableComponent>(targetUid, out var consumableComponent))
        {
            RaiseLocalEvent(targetUid, new SyncSuperMatterBoltStats());
            smComp.Matter += consumableComponent.AdditionalMatterOnConsumption;
            smComp.InternalEnergy += consumableComponent.AdditionalEnergyOnConsumption;
        }

        _audioSystem.PlayPvs(smComp.ConsumeSound, crystalUid);

        if (spawnEntity)
        {
            _popupSystem.PopupEntity(Loc.GetString("supermatter-consume", ("target", targetUid)), targetUid);
            var spawnedUid = EntityManager.SpawnEntity(smComp.ConsumeResultEntityPrototype, Transform(targetUid).Coordinates);
            if (HasComp<MetaDataComponent>(spawnedUid))
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendJoin(" ", [MetaData(spawnedUid).EntityName, Loc.GetString("supermatter-consume-preposition"), MetaData(targetUid).EntityName]);
                _metaData.SetEntityName(spawnedUid, stringBuilder.ToString());
            }
            else
                Log.Error($"Spawned Entity {spawnedUid} dont have MetaDataComponent");
        }
        EntityManager.QueueDeleteEntity(targetUid);
    }
}
