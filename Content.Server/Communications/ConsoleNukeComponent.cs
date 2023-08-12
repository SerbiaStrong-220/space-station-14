using Content.Server.UserInterface;
using Content.Shared.Communications;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Content.Shared.Communications;

namespace Content.Server.ConsoleNukeComponent
{
    [RegisterComponent]
    public sealed class ConsoleNukeComponent : Component
    {

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("isWarStarted")]
        public bool IsWarStarted = false;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("timeStampPush")]
        public int TimeStampPush;

    }
}
