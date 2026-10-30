// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Database;
using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Players;

/// <summary>
/// Sent server -> client to inform the client of their chats bans.
/// </summary>
public sealed class MsgChatsBans : NetMessage
{
    public override MsgGroups MsgGroup => MsgGroups.EntityEvent;

    public List<BannableChats> Bans = [];

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var count = buffer.ReadVariableInt32();
        Bans.EnsureCapacity(count);

        for (var i = 0; i < count; i++)
        {
            Bans.Add((BannableChats)buffer.ReadByte());
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.WriteVariableInt32(Bans.Count);

        foreach (var ban in Bans)
        {
            buffer.Write((byte)ban);
        }
    }
}
