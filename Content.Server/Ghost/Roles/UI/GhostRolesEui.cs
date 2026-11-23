using Content.Server.EUI;
using Content.Shared.Eui;
using Content.Shared.Ghost.Roles;

namespace Content.Server.Ghost.Roles.UI
{
    public sealed class GhostRolesEui : BaseEui
    {
        private readonly GhostRoleSystem _ghostRoleSystem;
        // ss220 add verb for ghost role start
        public uint? Identifier;
        public string? Rules;
        // ss220 add verb for ghost role end

        public GhostRolesEui()
        {
            _ghostRoleSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<GhostRoleSystem>();
        }

        public override EuiStateBase GetNewState() // ss220 add verb for ghost role
        {
            // ss220 add verb for ghost role start
            if (Identifier != null && Rules != null)
            {
                var state = new GhostRoleRuleEuiState(Identifier.Value, Rules);
                Identifier = null;
                Rules = null;
                return state;
            }

            return new GhostRolesEuiState(_ghostRoleSystem.GetGhostRolesInfo(Player));
            // ss220 add verb for ghost role end
        }

        public override void HandleMessage(EuiMessageBase msg)
        {
            base.HandleMessage(msg);

            switch (msg)
            {
                case RequestGhostRoleMessage req:
                    _ghostRoleSystem.Request(Player, req.Identifier);
                    break;
                case FollowGhostRoleMessage req:
                    _ghostRoleSystem.Follow(Player, req.Identifier);
                    break;
                case LeaveGhostRoleRaffleMessage req:
                    _ghostRoleSystem.LeaveRaffle(Player, req.Identifier);
                    break;
            }
        }

        public override void Closed()
        {
            base.Closed();

            _ghostRoleSystem.CloseEui(Player);
        }
    }
}
