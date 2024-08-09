// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Physics;
using Robust.Shared.Physics.Events;
using Content.Server.Popups;
using Content.Server.SS220.SuperMatterCrystal.Components;
using Content.Shared.Interaction;
using Content.Shared.Database;
using Content.Shared.Projectiles;
using Robust.Shared.Audio.Systems;

namespace Content.Server.SS220.SuperMatterCrystal;

public sealed partial class SuperMatterSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    private void InitializeInteractions()
    {
        // subscribe Events
        SubscribeLocalEvent<SuperMatterComponent, InteractHandEvent>(OnHandInteract);
        SubscribeLocalEvent<SuperMatterComponent, InteractUsingEvent>(OnItemInteract);
        SubscribeLocalEvent<SuperMatterComponent, StartCollideEvent>(OnCollideEvent);
        SubscribeLocalEvent<SuperMatterComponent, SuperMatterActivationEvent>(OnActivationEvent);
        SubscribeLocalEvent<SuperMatterComponent, SuperMatterSetAdminDisableEvent>(OnAdminDisableEvent);
    }
    private void OnHandInteract(Entity<SuperMatterComponent> entity, ref InteractHandEvent args)
    {
        entity.Comp.Matter += MatterNondimensionalization;
        ConsumeObject(args.User, entity);
    }
    private void OnItemInteract(Entity<SuperMatterComponent> entity, ref InteractUsingEvent args)
    {
        entity.Comp.Matter += MatterNondimensionalization / 4;
        ConsumeObject(args.User, entity);
    }
    private void OnCollideEvent(Entity<SuperMatterComponent> entity, ref StartCollideEvent args)
    {
        if (args.OtherBody.BodyType == BodyType.Static)
            return;
        if (TryComp<ProjectileComponent>(args.OtherEntity, out var projectile))
            entity.Comp.InternalEnergy += CHEMISTRY_POTENTIAL_BASE * MathF.Max((float) projectile.Damage.GetTotal(), 0f);

        entity.Comp.Matter += MatterNondimensionalization / 4;
        ConsumeObject(args.OtherEntity, entity, HasComp<ProjectileComponent>(args.OtherEntity));
    }
    private void ConsumeObject(EntityUid targetUid, Entity<SuperMatterComponent> crystal, bool spawnEntity = true)
    {
        var (crystalUid, smComp) = crystal;

        if (smComp.DisabledByAdmin)
            return;
        if (HasComp<SuperMatterImmuneComponent>(targetUid))
            return;
        if (!smComp.Activated)
        {
            var ev = new SuperMatterActivationEvent(crystalUid, crystalUid);
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
    private void OnActivationEvent(Entity<SuperMatterComponent> entity, ref SuperMatterActivationEvent args)
    {
        if (!TryComp<SuperMatterComponent>(args.Target, out var smComp))
        {
            Log.Error($"Tried to activate SM entity {EntityManager.ToPrettyString(args.Target)} without SuperMatterComponent, activationEvent performer {EntityManager.ToPrettyString(args.Performer)}");
            return;
        }
        if (smComp.DisabledByAdmin)
            return;
        if (!smComp.Activated)
        {
            SendAdminChatAlert(entity, "supermatter-activated", $"{EntityManager.ToPrettyString(args.Target)}");
            smComp.Activated = true;
        }
        args.Handled = true;
    }
    private void OnAdminDisableEvent(Entity<SuperMatterComponent> entity, ref SuperMatterSetAdminDisableEvent args)
    {
        if (!TryComp<SuperMatterComponent>(args.Target, out var smComp))
        {
            Log.Error($"Tried to AdminDisable SM entity {EntityManager.ToPrettyString(args.Target)} without SuperMatterComponent, activationEvent performer {EntityManager.ToPrettyString(args.Performer)}");
            return;
        }
        _adminLog.Add(LogType.Verb, LogImpact.Extreme, $"{EntityManager.ToPrettyString(args.Performer):player} has set AdminDisable to {args.AdminDisableValue}");
        smComp.DisabledByAdmin = args.AdminDisableValue;
        // TODO other logic like freezing Delamination and etc etc
    }
}
