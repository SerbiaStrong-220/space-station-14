using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Utility;

namespace Content.Client.Administration.Components
{
    [RegisterComponent]
    public sealed class AGhostComponent : Component
    {

        public EntityTargetAction ToggleGhostRoleCastAction = new()
        {
            Icon = new SpriteSpecifier.Texture(new("Corvax/Interface/AdminActions/GhostRoleCast.png")),
            DisplayName = "aghost-toggle-ghostrole-cast-name",
            Description = "aghost-toggle-ghostrole-cast-desc",
            ClientExclusive = true,
            CheckCanInteract = false,
            Priority = -8,
            Repeat = true,
            DeselectOnMiss = false,
            //CanTargetSelf = false,
            Event = new ToggleGhostRoleCastActionEvent(),
        };

        public EntityTargetAction ToggleGhostRoleRemoveAction = new()
        {
            Icon = new SpriteSpecifier.Texture(new("Corvax/Interface/AdminActions/GhostRoleRemove.png")),
            DisplayName = "aghost-toggle-ghostrole-remove-name",
            Description = "aghost-toggle-ghostrole-remove-desc",
            ClientExclusive = true,
            CheckCanInteract = false,
            Priority = -9,
            Repeat = true,
            DeselectOnMiss = false,
            //CanTargetSelf = false,
        Event = new ToggleGhostRoleRemoveActionEvent(),
        };
    }


    public sealed class ToggleGhostRoleCastActionEvent : EntityTargetActionEvent { };
    public sealed class ToggleGhostRoleRemoveActionEvent : EntityTargetActionEvent { };

}
