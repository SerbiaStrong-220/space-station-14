using Content.Shared.Communications;
using Robust.Shared.Timing;
using Robust.Shared.GameObjects;
using static Content.Shared.Access.Components.AccessOverriderComponent;
using System.ComponentModel;

namespace Content.Shared.SS220.CluwneComms
{
    public abstract class SharedCluwneCommsConsoleSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<CluwneCommsConsoleComponent, MapInitEvent>(OnMapInit);
        }
        public void OnMapInit(Entity<CluwneCommsConsoleComponent> ent, ref MapInitEvent args)
        {
            ent.Comp.AnnouncementCooldownRemaining = _timing.CurTime + ent.Comp.Delay;
            ent.Comp.CanAnnounce = false;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<CluwneCommsConsoleComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                if (!comp.CanAnnounce && _timing.CurTime >= comp.AnnouncementCooldownRemaining)
                {
                    comp.CanAnnounce = true;
                    UpdateUI(uid, comp);
                }
            }
        }

        private void UpdateUI(EntityUid ent, CluwneCommsConsoleComponent comp)
        {
            CluwneCommsConsoleInterfaceState newState = new CluwneCommsConsoleInterfaceState(comp.CanAnnounce);

            _uiSystem.SetUiState(ent, CluwneCommsConsoleUiKey.Key, newState);
        }
    }
}
