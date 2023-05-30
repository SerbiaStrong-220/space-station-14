using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Eye.SecurityHud.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;

namespace Content.Shared.Eye.SecurityHud.Systems
{
    public sealed class SecurityHudSystem : EntitySystem
    {
        [Dependency] private readonly HudableSystem _hudableSystem = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<SecurityHudComponent, GotEquippedEvent>(OnEquipped);
            SubscribeLocalEvent<SecurityHudComponent, GotUnequippedEvent>(OnUnequipped);
            SubscribeLocalEvent<SecurityHudComponent, InventoryRelayedEvent<CanSeeHudAttemptEvent>>(OnHudTrySee);
        }

        private void OnHudTrySee(EntityUid uid, SecurityHudComponent component, InventoryRelayedEvent<CanSeeHudAttemptEvent> args)
        {
            args.Args.Cancel();
        }

        private void OnEquipped(EntityUid uid, SecurityHudComponent component, GotEquippedEvent args)
        {
            _hudableSystem.UpdateIsOn(args.Equipee);
        }

        private void OnUnequipped(EntityUid uid, SecurityHudComponent component, GotUnequippedEvent args)
        {
            _hudableSystem.UpdateIsOn(args.Equipee);
        }
    }
}
