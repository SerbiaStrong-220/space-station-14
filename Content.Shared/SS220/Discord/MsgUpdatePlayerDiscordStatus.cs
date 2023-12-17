using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Discord;

public sealed class MsgUpdatePlayerDiscordStatus : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.Command;

    public DiscordSponsorInfo? Info { get; set; }

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        if (buffer.ReadBoolean())
        {
            Info = new DiscordSponsorInfo
            {
                Tier = (SponsorTier) buffer.ReadUInt32()
            };
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.Write(Info is not null);

        if (Info == null)
        {
            return;
        }

        buffer.Write((uint) Info.Tier);
    }
}
