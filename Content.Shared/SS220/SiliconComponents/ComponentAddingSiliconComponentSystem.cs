// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

namespace Content.Shared.SS220.SiliconComponents;

public sealed partial class ComponentAddingSiliconComponentSystem : EntitySystem
{
    [Dependency] private IEntityManager _entManager = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ComponentAddingSiliconPartComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<ComponentAddingSiliconPartComponent, ComponentShutdown>(OnComponentShutdown);

        SubscribeLocalEvent<ComponentAddingSiliconPartComponent, ComponentGotInsertedIntoUser>(OnEntityInserted);
        SubscribeLocalEvent<ComponentAddingSiliconPartComponent, ComponentGotRemovedFromUser>(OnEntityRemoved);

        SubscribeLocalEvent<ComponentAddingSiliconPartComponent, SiliconPartStatusOnline>(OnPartOnline);
        SubscribeLocalEvent<ComponentAddingSiliconPartComponent, SiliconPartStatusOffline>(OnPartOffline);

        SubscribeLocalEvent<ComponentAddingSiliconModuleComponent, SiliconModuleGotInserted>(OnModuleInserted);
        SubscribeLocalEvent<ComponentAddingSiliconModuleComponent, SiliconModuleGotRemoved>(OnModuleRemoved);
    }

    private void OnEntityInserted(Entity<ComponentAddingSiliconPartComponent> ent, ref ComponentGotInsertedIntoUser args)
    {
        if (!HasComp<SiliconComponentsComponent>(args.Owner))
            return;

        if (!TryComp<SiliconPartComponent>(ent.Owner, out var partComp) || partComp.PartOwner is not { Valid: true } || !partComp.Active)
            return;

        _entManager.AddComponents(args.Owner, ent.Comp.Components);
    }

    private void OnEntityRemoved(Entity<ComponentAddingSiliconPartComponent> ent, ref ComponentGotRemovedFromUser args)
    {
        if (!TryComp<SiliconComponentsComponent>(args.Owner, out var ownerComp))
            return;

        _entManager.RemoveComponents(args.Owner, ent.Comp.Components);
    }

    private void OnComponentStartup(Entity<ComponentAddingSiliconPartComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<SiliconPartComponent>(ent.Owner, out var partComp) || partComp.PartOwner is not { Valid: true } ownerValidated || !partComp.Active)
            return;

        if (!HasComp<SiliconComponentsComponent>(partComp.PartOwner))
            return;

        _entManager.AddComponents(ownerValidated, ent.Comp.Components);
    }

    private void OnComponentShutdown(Entity<ComponentAddingSiliconPartComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<SiliconPartComponent>(ent.Owner, out var partComp) || partComp.PartOwner is not { Valid: true } ownerValidated || !partComp.Active)
            return;

        if (!HasComp<SiliconComponentsComponent>(partComp.PartOwner))
            return;

        _entManager.RemoveComponents(ownerValidated, ent.Comp.Components);
    }

    private void OnPartOnline(Entity<ComponentAddingSiliconPartComponent> ent, ref SiliconPartStatusOnline args)
    {
        if (!TryComp<SiliconPartComponent>(ent.Owner, out var partComp) || partComp.PartOwner is not { Valid: true } ownerValidated || !partComp.Active)
            return;

        if (!HasComp<SiliconComponentsComponent>(partComp.PartOwner))
            return;

        _entManager.AddComponents(ownerValidated, ent.Comp.Components);
    }

    private void OnPartOffline(Entity<ComponentAddingSiliconPartComponent> ent, ref SiliconPartStatusOffline args)
    {
        if (!TryComp<SiliconPartComponent>(ent.Owner, out var partComp) || partComp.PartOwner is not { Valid: true } ownerValidated || !partComp.Active)
            return;

        if (!HasComp<SiliconComponentsComponent>(partComp.PartOwner))
            return;

        _entManager.RemoveComponents(ownerValidated, ent.Comp.Components);
    }

    private void OnModuleInserted(Entity<ComponentAddingSiliconModuleComponent> ent, ref SiliconModuleGotInserted args)
    {
        if (!HasComp<SiliconComponentsComponent>(args.Owner))
            return;


        _entManager.AddComponents(args.Owner, ent.Comp.Components);
    }

    private void OnModuleRemoved(Entity<ComponentAddingSiliconModuleComponent> ent, ref SiliconModuleGotRemoved args)
    {
        if (!TryComp<SiliconComponentsComponent>(args.Owner, out var ownerComp))
            return;

        _entManager.RemoveComponents(args.Owner, ent.Comp.Components);
    }
}
