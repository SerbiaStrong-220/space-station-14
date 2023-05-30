using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Eye.SecurityHud.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Eye.SecurityHud.Components
{
    [RegisterComponent]
    [NetworkedComponent, AutoGenerateComponentState]
    [Access(typeof(HudableSystem))]
    public sealed partial class HudableComponent : Component
    {
        /// <summary>
        /// How many seconds will be subtracted from each attempt to add blindness to us?
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("isVisible"), AutoNetworkedField]
        public bool IsVisible;

        /// <description>
        /// Used to ensure that this doesn't break with sandbox or admin tools.
        /// This is not "enabled/disabled".
        /// </description>
        [Access(Other = AccessPermissions.ReadWriteExecute)]
        public bool LightSetup = false;

        /// <description>
        /// Gives an extra frame of blindness to reenable light manager during
        /// </description>
        [Access(Other = AccessPermissions.ReadWriteExecute)]
        public bool GraceFrame = false;
    }
}
