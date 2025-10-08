// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.SmartFridge;
using Content.Server.Power.EntitySystems;
using Content.Shared.UserInterface;
using Content.Shared.Destructible;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Content.Shared.Power;

namespace Content.Server.SS220.SmartFridge
{
    public sealed class SmartFridgeSystem : SharedSmartFridgeSystem
    {
        [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;

        private ISawmill _sawmill = default!;

        public override void Initialize()
        {
            base.Initialize();

            _sawmill = Logger.GetSawmill("smartfridge");
            SubscribeLocalEvent<SmartFridge220Component, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<SmartFridge220Component, ActivatableUIOpenAttemptEvent>(OnActivatableUIOpenAttempt);
            SubscribeLocalEvent<SmartFridge220Component, BreakageEventArgs>(OnBreak);

        }
        private void OnActivatableUIOpenAttempt(EntityUid uid, SmartFridge220Component component, ActivatableUIOpenAttemptEvent args)
        {
            if (component.Broken)
                args.Cancel();
        }
        private void OnPowerChanged(EntityUid uid, SmartFridge220Component component, ref PowerChangedEvent args)
        {
            TryUpdateVisualState(uid, component);
        }

        private void OnBreak(EntityUid uid, SmartFridge220Component component, BreakageEventArgs eventArgs)
        {
            component.Broken = true;
            TryUpdateVisualState(uid, component);
        }
        public void Deny(EntityUid uid, SmartFridge220Component? component = null)
        {
            if (!Resolve(uid, ref component))
                return;

            if (component.Denying)
                return;

            component.Denying = true;
            Audio.PlayPvs(component.SoundDeny, uid, AudioParams.Default.WithVolume(-2f));
            TryUpdateVisualState(uid, component);
        }

        /// <summary>
        /// Tries to update the visuals of the component based on its current state.
        /// </summary>
        public void TryUpdateVisualState(EntityUid uid, SmartFridge220Component? vendComponent = null)
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
    }
}
