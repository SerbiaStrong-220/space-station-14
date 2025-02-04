// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.IO;
using Lidgren.Network;
using Robust.Shared.Audio;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.TTS;

public sealed class MsgPlayAnnounceTts : NetMessage
{
    public TtsAudioData Data { get; set; }
    public string AnnouncementSound { get; set; } = "";
    public AudioParams AnnouncementParams { get; set; } = AudioParams.Default;

    public override MsgGroups MsgGroup => MsgGroups.Command;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var data = new TtsAudioData();
        data.ReadFromNetBuffer(buffer);
        Data = data;
        AnnouncementSound = buffer.ReadString();

        var streamLength = buffer.ReadVariableInt32();
        using var stream = new MemoryStream(streamLength);
        buffer.ReadAlignedMemory(stream, streamLength);
        {
            AnnouncementParams = serializer.Deserialize<AudioParams>(stream);
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        Data.WriteToNetBuffer(buffer);
        buffer.Write(AnnouncementSound);

        using var stream = new MemoryStream();
        {
            serializer.Serialize(stream, AnnouncementParams);
        }
        var streamLength = (int)stream.Length;
        buffer.WriteVariableInt32(streamLength);
        buffer.Write(stream.GetBuffer().AsSpan(0, streamLength));
    }
}
