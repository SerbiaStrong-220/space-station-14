using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Rejuvenate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Eye.SecurityHud.Components;
using Content.Shared.Inventory;
using JetBrains.Annotations;
using Content.Shared.Eye.Blinding.Systems;

namespace Content.Shared.Eye.SecurityHud.Systems
{
    public sealed class HudableSystem : EntitySystem
    {

        public override void Initialize()
        {
            base.Initialize();
        }
        [PublicAPI]
        public void UpdateIsOn(EntityUid uid, HudableComponent? hudable = null)
        {
            if (!Resolve(uid, ref hudable, false))
                return;

            var old = hudable.IsVisible;

            var ev = new CanSeeHudAttemptEvent();
            RaiseLocalEvent(uid, ev);
            hudable.IsVisible = ev.Visible;

            if (old == hudable.IsVisible)
                return;

            var changeEv = new HudChangedEvent(hudable.IsVisible);
            RaiseLocalEvent(uid, ref changeEv);
            Dirty(hudable);
        }
    }
    public sealed class CanSeeHudAttemptEvent : CancellableEntityEventArgs, IInventoryRelayEvent
    {
        public bool Visible => Cancelled;
        public SlotFlags TargetSlots => SlotFlags.EYES | SlotFlags.MASK | SlotFlags.HEAD;
    }

    [ByRefEvent]
    public record struct HudChangedEvent(bool Visible);
}
