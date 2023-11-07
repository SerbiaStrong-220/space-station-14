using System;
using System.Collections.Generic;
using Content.Client.Eui;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client.SS220.UserInterface.DiscordLink
{
    [UsedImplicitly]
    public sealed class DiscordLinkEui : BaseEui
    {
        private DiscordLinkWindow DiscordWindow { get; }

        public DiscordLinkEui()
        {
            DiscordWindow = new DiscordLinkWindow();
        }

        public override void Opened()
        {
            DiscordWindow.OpenCentered();
        }

        public override void HandleState(EuiStateBase state)
        {
        }
    }
}
