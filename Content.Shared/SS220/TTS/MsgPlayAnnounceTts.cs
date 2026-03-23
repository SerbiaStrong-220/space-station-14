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
    public SoundSpecifier AnnouncementSound { get; set; } = new SoundPathSpecifier("");
    public AudioWithTTSPlayOperation PlayAudioMask = AudioWithTTSPlayOperation.PlayAll;

    public override MsgGroups MsgGroup => MsgGroups.Command;

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var data = new TtsAudioData();
        data.ReadFromNetBuffer(buffer);
        Data = data;

        var streamLength = buffer.ReadVariableInt32();
        using var stream = new MemoryStream(streamLength);
        buffer.ReadAlignedMemory(stream, streamLength);
        {
            AnnouncementSound = serializer.Deserialize<SoundSpecifier>(stream);
        }

        PlayAudioMask = (AudioWithTTSPlayOperation)buffer.ReadByte();
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        Data.WriteToNetBuffer(buffer);

        using var stream = new MemoryStream();
        {
            serializer.Serialize(stream, AnnouncementSound);
        }
        var streamLength = (int)stream.Length;
        buffer.WriteVariableInt32(streamLength);
        buffer.Write(stream.GetBuffer().AsSpan(0, streamLength));

        buffer.Write((byte)PlayAudioMask);
    }
}

[Flags]
public enum AudioWithTTSPlayOperation : byte
{
    NotPlay = 1 << 0,
    PlayAudio = 1 << 1,
    PlayTTS = 1 << 2,

    PlayAll = PlayAudio | PlayTTS,
}
