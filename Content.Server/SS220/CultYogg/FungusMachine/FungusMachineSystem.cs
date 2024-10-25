// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Popups;
using Content.Shared.SS220.CultYogg.MiGo;
using Content.Shared.SS220.CultYogg.FungusMachine;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;

namespace Content.Server.SS220.CultYogg.FungusMachine
{
    public sealed class FungusMachineSystem : SharedFungusMachineSystem
    {
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<FungusMachineComponent, ActivatableUIOpenAttemptEvent>(OnAttemptOpenUI);
            Subs.BuiEvents<FungusMachineComponent>(FungusMachineUiKey.Key, subs =>
            {
                subs.Event<BoundUIOpenedEvent>(OnBoundUIOpened);
            });
        }

        private void OnAttemptOpenUI(Entity<FungusMachineComponent> ent, ref ActivatableUIOpenAttemptEvent args)
        {
            if (HasComp<MiGoComponent>(args.User))
                return;

            _popupSystem.PopupEntity(Loc.GetString("cult-yogg-fungus-denied-to-use"), ent, args.User);
            args.Cancel();
        }

        protected override void OnComponentInit(EntityUid uid, FungusMachineComponent component, ComponentInit args)
        {
            base.OnComponentInit(uid, component, args);

            component.Container = _containerSystem.EnsureContainer<Container>(uid, FungusMachineComponent.ContainerId);
        }

        private void OnBoundUIOpened(EntityUid uid, FungusMachineComponent component, BoundUIOpenedEvent args)
        {
            UpdateFungusMachineInterfaceState(uid, component);
        }

        private void UpdateFungusMachineInterfaceState(EntityUid uid, FungusMachineComponent component)
        {
            var state = new FungusMachineInterfaceState(GetInventory(uid, component));

            _userInterfaceSystem.SetUiState(uid, FungusMachineUiKey.Key, state);
        }

        private FungusMachineInventoryEntry? GetEntry(EntityUid uid, string entryId, FungusMachineComponent? component = null)
        {
            return !Resolve(uid, ref component) ? null : component.Inventory.GetValueOrDefault(entryId);
        }
    }
}

