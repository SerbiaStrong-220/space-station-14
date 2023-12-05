using Content.Shared.SS220.SmartFridge;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.UserInterface;
using Content.Shared.Destructible;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;

using System.Linq;
using Content.Server.Administration.Logs;
using System.Numerics;
using Content.Server.Cargo.Systems;
using Content.Server.Emp;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.UserInterface;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Emp;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Content.Shared.VendingMachines;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;



namespace Content.Server.SS220.SmartFridge
{
    public sealed class SmartFridgeSystem : SharedSmartFridgeSystem
    {
        [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;

        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly AccessReaderSystem _accessReader = default!;
        [Dependency] private readonly SharedActionsSystem _action = default!;
        [Dependency] private readonly PricingSystem _pricing = default!;
        [Dependency] private readonly ThrowingSystem _throwingSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
        [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
        [Dependency] private readonly IAdminLogManager _adminLogger = default!;

        private ISawmill _sawmill = default!;

        public override void Initialize()
        {
            base.Initialize();

            _sawmill = Logger.GetSawmill("smartfridge");
            SubscribeLocalEvent<SmartFridgeComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<SmartFridgeComponent, ActivatableUIOpenAttemptEvent>(OnActivatableUIOpenAttempt);
            SubscribeLocalEvent<SmartFridgeComponent, BreakageEventArgs>(OnBreak);

        }
        private void OnActivatableUIOpenAttempt(EntityUid uid, SmartFridgeComponent component, ActivatableUIOpenAttemptEvent args)
        {
            if (component.Broken)
                args.Cancel();
        }
        private void OnPowerChanged(EntityUid uid, SmartFridgeComponent component, ref PowerChangedEvent args)
        {
            TryUpdateVisualState(uid, component);
        }

        private void OnBreak(EntityUid uid, SmartFridgeComponent component, BreakageEventArgs eventArgs)
        {
            component.Broken = true;
            TryUpdateVisualState(uid, component);
        }
        public void Deny(EntityUid uid, SmartFridgeComponent? сomponent = null)
        {
            if (!Resolve(uid, ref сomponent))
                return;

            if (сomponent.Denying)
                return;

            сomponent.Denying = true;
            Audio.PlayPvs(сomponent.SoundDeny, uid, AudioParams.Default.WithVolume(-2f));
            TryUpdateVisualState(uid, сomponent);
        }

        /// <summary>
        /// Tries to update the visuals of the component based on its current state.
        /// </summary>
        public void TryUpdateVisualState(EntityUid uid, SmartFridgeComponent? vendComponent = null)
        {
            if (!Resolve(uid, ref vendComponent))
                return;

            var finalState = SmartFridgeVisualState.Normal;
            if (vendComponent.Broken)
            {
                finalState = SmartFridgeVisualState.Broken;
            }
            else if (vendComponent.Denying)
            {
                finalState = SmartFridgeVisualState.Deny;
            }
            else if (!this.IsPowered(uid, EntityManager))
            {
                finalState = SmartFridgeVisualState.Off;
            }

            _appearanceSystem.SetData(uid, SmartFridgeVisuals.VisualState, finalState);
        }
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<SmartFridgeComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                if (comp.Denying)
                {
                    comp.DenyAccumulator += frameTime;
                    if (comp.DenyAccumulator >= comp.DenyDelay)
                    {
                        comp.DenyAccumulator = 0f;
                        comp.Denying = false;

                        TryUpdateVisualState(uid, comp);
                    }
                }

                if (comp.DispenseOnHitCoolingDown)
                {
                    comp.DispenseOnHitAccumulator += frameTime;
                    if (comp.DispenseOnHitAccumulator >= comp.DispenseOnHitCooldown)
                    {
                        comp.DispenseOnHitAccumulator = 0f;
                        comp.DispenseOnHitCoolingDown = false;
                    }
                }
            }
        }
    }
}
