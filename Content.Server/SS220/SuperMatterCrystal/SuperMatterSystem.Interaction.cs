// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Physics;
using Robust.Shared.Physics.Events;
using Content.Server.SS220.SuperMatterCrystal.Components;
using Content.Shared.Interaction;
using Content.Shared.Projectiles;

namespace Content.Server.SS220.SuperMatterCrystal;

public sealed partial class SuperMatterSystem : EntitySystem
{
    private void InitializeInteractions()
    {
        // subscribe Events
        SubscribeLocalEvent<SuperMatterComponent, InteractHandEvent>(OnHandInteract);
        SubscribeLocalEvent<SuperMatterComponent, InteractUsingEvent>(OnItemInteract);
        SubscribeLocalEvent<SuperMatterComponent, StartCollideEvent>(OnCollideEvent);
    }
    private void OnHandInteract(Entity<SuperMatterComponent> entity, ref InteractHandEvent args)
    {
        entity.Comp.Matter += MatterNondimensionalization;
        ConsumeObject(args.User, entity.Comp);
    }
    private void OnItemInteract(Entity<SuperMatterComponent> entity, ref InteractUsingEvent args)
    {
        entity.Comp.Matter += MatterNondimensionalization / 4;
        ConsumeObject(args.User, entity.Comp);
    }
    private void OnCollideEvent(Entity<SuperMatterComponent> entity, ref StartCollideEvent args)
    {
        if (args.OtherBody.BodyType == BodyType.Static)
            return;
        if (TryComp<ProjectileComponent>(args.OtherEntity, out var projectile))
            entity.Comp.InternalEnergy += CHEMISTRY_POTENTIAL_BASE * MathF.Max((float) projectile.Damage.GetTotal(), 0f);
        entity.Comp.Matter += MatterNondimensionalization / 4;
        ConsumeObject(args.OtherEntity, entity.Comp);
    }
    private void ConsumeObject(EntityUid uid, SuperMatterComponent smComp)
    {
        if (smComp.DisabledByAdmin)
            return;
        if (HasComp<SuperMatterImmuneComponent>(uid))
            return;
        if (!smComp.Activated)
            smComp.Activated = true;
        if (TryComp<SuperMatterSpecificConsumableComponent>(uid, out var consumableComponent))
            smComp.Matter += consumableComponent.AdditionalMatterOnConsumption;
        // TODO: Spawn Ash & make sound/popup
        EntityManager.QueueDeleteEntity(uid);
    }
}
