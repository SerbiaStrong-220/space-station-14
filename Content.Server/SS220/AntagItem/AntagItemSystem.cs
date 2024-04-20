using Content.Server.GameTicking.Rules.Components;
using Content.Server.Hands.Systems;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Roles;
using Content.Shared.Damage;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Mobs.Systems;
using Content.Shared.Roles;
using Content.Shared.Storage.Components;
using Content.Shared.Weapons.Reflect;
using Robust.Shared.Containers;

namespace Content.Server.SS220.AntagItem
{
    public sealed partial class AntagItemSystem : EntitySystem
    {
        [Dependency] private readonly MindSystem _mind = default!;
        [Dependency] private readonly RoleSystem _role = default!;
        [Dependency] private readonly HandsSystem _hands = default!;
        [Dependency] private readonly DamageableSystem _damage = default!;
        [Dependency] private readonly PopupSystem _popup = default!;
        public override void Initialize()
        {

            SubscribeLocalEvent<AntagItemComponent, BeingEquippedAttemptEvent>(OnEquipAttempt);
            SubscribeLocalEvent<AntagItemComponent, GotEquippedHandEvent>(GotEquippedHand);
        }


        private void GotEquippedHand(EntityUid uid, AntagItemComponent component, GotEquippedHandEvent ev)
        {
            if (!_mind.TryGetMind(ev.User, out var usermindId, out var mind))
                return;
            if (HasComp<AntagItemComponent>(ev.Equipped) && !_role.MindHasRole<TraitorRoleComponent>(usermindId))
            {
                _hands.TryDrop(ev.User);
                _damage.TryChangeDamage(ev.User, component.Damage);
                _popup.PopupEntity(component.DropText, ev.Equipped, type: Shared.Popups.PopupType.LargeCaution);
            }
        }

        private void OnEquipAttempt(EntityUid uid, AntagItemComponent component, BeingEquippedAttemptEvent ev)
        {
            if (HasComp<AntagItemComponent>(ev.Equipment))
            {
                if (!_mind.TryGetMind(ev.EquipTarget, out var mindId, out var mind))
                {
                    ev.Cancel();
                    return;
                }

                if (!_role.MindHasRole<TraitorRoleComponent>(mindId))
                {
                    ev.Cancel();
                    _popup.PopupEntity(component.DropText, ev.Equipment, type: Shared.Popups.PopupType.MediumCaution);
                }
            }
        }


    }
}
