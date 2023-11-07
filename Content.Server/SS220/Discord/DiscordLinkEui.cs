using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Server.EUI;

namespace Content.Server.SS220.Discord
{
    public sealed class DiscordLinkEui : BaseEui
    {
        public DiscordLinkEui()
        {
            IoCManager.InjectDependencies(this);
        }
    }
}
