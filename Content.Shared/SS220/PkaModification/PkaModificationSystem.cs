using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Spawners;
using Robust.Shared.Prototypes;
using Content.Shared.Projectiles;
using Content.Shared.Damage;
using Robust.Shared.GameObjects;
using System.ComponentModel.Design;

namespace Content.Shared.SS220.PkaModification
{
    internal class PkaModificationSystem : EntitySystem
    {
        public override void Initialize()
        {

            base.Initialize();

            SubscribeLocalEvent<PkaModificationComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<PkaModificationComponent, TakeAmmoEvent>(OnAmmo);
        }

        private void OnAmmo(Entity<PkaModificationComponent> ent, ref TakeAmmoEvent args)
        {
            if (!TryComp<BasicEntityAmmoProviderComponent>(ent, out var ammoComponent))
                return;

            var entBullet = ammoComponent.SavedUid;

            if (!TryComp<TimedDespawnComponent>(entBullet, out var longBullet))
                return;

            longBullet.Lifetime = 0.3f;
        }

        private void OnInteractUsing(Entity<PkaModificationComponent> ent, ref InteractUsingEvent args)
        {
            if (!TryComp<RechargeBasicEntityAmmoComponent>(ent, out var rechargeComponent))
                return;

            rechargeComponent.RechargeCooldown = 10.0f;
        }
    }
}
