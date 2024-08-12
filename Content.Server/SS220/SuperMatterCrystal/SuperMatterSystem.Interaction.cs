// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Popups;
using Content.Server.SS220.SuperMatterCrystal.Components;
using Robust.Shared.Audio.Systems;

namespace Content.Server.SS220.SuperMatterCrystal;

public sealed partial class SuperMatterSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    public void ConsumeObject(EntityUid targetUid, Entity<SuperMatterComponent> crystal, bool spawnEntity = true)
    {
        var (crystalUid, smComp) = crystal;

        if (smComp.DisabledByAdmin)
            return;
        if (HasComp<SuperMatterImmuneComponent>(targetUid))
            return;
        if (!smComp.Activated)
        {
            var ev = new SuperMatterActivationEvent(crystalUid, targetUid);
            RaiseLocalEvent(crystalUid, ev);
        }
        if (TryComp<SuperMatterSpecificConsumableComponent>(targetUid, out var consumableComponent))
            smComp.Matter += consumableComponent.AdditionalMatterOnConsumption;

        _popupSystem.PopupEntity(Loc.GetString("supermatter-consume", ("target", targetUid)), targetUid);
        _audioSystem.PlayPvs(smComp.ConsumeSound, crystalUid);
        if (spawnEntity)
            EntityManager.SpawnEntity(smComp.ConsumeResultEntityPrototype, Transform(targetUid).Coordinates);
        EntityManager.QueueDeleteEntity(targetUid);
    }
}
