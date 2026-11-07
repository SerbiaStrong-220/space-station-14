// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.Actions;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction.Components;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.SiliconComponents;

public sealed partial class SiliconModuleSystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly LocId HandUnavailable = "silicon-installation-hand-unavailable";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SiliconModuleArmInstallationComponent, SiliconModuleGotInserted>(OnSelectableInstalled);
        SubscribeLocalEvent<SiliconModuleArmInstallationComponent, SiliconModuleGotRemoved>(OnSelectableRemoved);

        SubscribeLocalEvent<SiliconModuleArmInstallationComponent, SiliconModuleArmInstallationToggledEvent>(OnSelectableAction);
        SubscribeLocalEvent<SiliconModuleArmInstallationComponent, ComponentStartup>(OnInstallationStartup);
    }

    public void OnSelectableAction(Entity<SiliconModuleArmInstallationComponent> ent, ref SiliconModuleArmInstallationToggledEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!TryComp<SiliconModuleComponent>(ent.Owner, out var modComp) || modComp.ModuleOwner is not { Valid: true } modOwnerValidated)
            return;

        if (!_container.TryGetContainer(ent.Owner, ent.Comp.HoldingContainerPrefix + "_" + ent.Comp.HoldingContainerHandId, out var containerInstallation) ||
            containerInstallation is not ContainerSlot containerInstallationVerified)
            return;

        if (containerInstallationVerified.ContainedEntity == null)
        {
            RemComp<UnremoveableComponent>(ent.Comp.StoredItem);
            _container.Insert(ent.Comp.StoredItem, containerInstallationVerified);

            if (_net.IsServer)
                _audio.PlayPvs(ent.Comp.ToggleSound, modOwnerValidated);

            Dirty(ent);
            return;
        }

        if (_container.TryGetContainer(modOwnerValidated, ent.Comp.HoldingContainerHandId, out var container) &&
            container is ContainerSlot handContainer &&
            handContainer.ContainedEntity == null &&
            containerInstallationVerified.ContainedEntity is { Valid: true } itemValid)
        {
            _container.Insert(itemValid, handContainer);
            EnsureComp<UnremoveableComponent>(itemValid);

            if (_net.IsServer)
                _audio.PlayPvs(ent.Comp.ToggleSound, modOwnerValidated);

            Dirty(ent);
            return;
        }

        _popup.PopupEntity(Loc.GetString(HandUnavailable), ent);
        return;
    }

    public void OnInstallationStartup(Entity<SiliconModuleArmInstallationComponent> ent, ref ComponentStartup args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var container = _container.EnsureContainer<ContainerSlot>(ent.Owner, ent.Comp.HoldingContainerPrefix + "_" + ent.Comp.HoldingContainerHandId);

        var xform = Transform(ent);

        var item = PredictedSpawnAtPosition(ent.Comp.Item, xform.Coordinates);

        ent.Comp.StoredItem = item;

        _container.Insert(item, container);

        Dirty(ent);
    }

    public void OnSelectableInstalled(Entity<SiliconModuleArmInstallationComponent> ent, ref SiliconModuleGotInserted args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (_actions.AddAction(args.Owner, ref ent.Comp.InstallationToggleEntity, out var action, ent.Comp.InstallationToggleAction, ent.Owner) &&
            ent.Comp.InstallationToggleEntity is { Valid: true } installationActionEntValid)
            _actions.SetEntityIcon(installationActionEntValid, ent.Comp.StoredItem);

        Dirty(ent);
    }

    public void OnSelectableRemoved(Entity<SiliconModuleArmInstallationComponent> ent, ref SiliconModuleGotRemoved args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        _actions.RemoveProvidedActions(args.Owner, ent.Owner);

        Dirty(ent);
    }

    public bool CanInsertModule(Entity<SiliconComponentsComponent> user, Entity<SiliconModuleComponent> module)
    {
        if (!TryComp<SiliconModuleBlacklistComponent>(module, out var whitelistComp))
            return false;

        if (user.Comp.ModuleContainer == null)
            return false;

        foreach (var mod in user.Comp.ModuleContainer.ContainedEntities)
        {
            if (whitelistComp != null &&
                _whitelist.IsWhitelistPass(whitelistComp.ModuleBlacklist, mod))
                return false;

            if (TryComp<SiliconModuleBlacklistComponent>(mod, out var modWhitelistComp) &&
                _whitelist.IsWhitelistPass(modWhitelistComp.ModuleBlacklist, module))
                return false;
        }


        return true;
    }

}

[ByRefEvent]
public record struct SiliconModuleGotInserted(EntityUid Owner)
{
}

[ByRefEvent]
public record struct SiliconModuleGotRemoved(EntityUid Owner)
{
}

[ByRefEvent]
public record struct SiliconModuleInserted(EntityUid Module)
{
}

[ByRefEvent]
public record struct SiliconModuleRemoved(EntityUid Module)
{
}


