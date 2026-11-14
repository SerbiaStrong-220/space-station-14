// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.TTS.Systems;
using Lidgren.Network;
using Robust.Shared.Audio;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using System.IO;

namespace Content.Shared.SS220.TTS;

public struct PlayTtsMessageData
{
    public required ITtsData TtsData;
    public required TtsMetadata TtsMetadata;
}

/// <summary>
/// Message to play one or several TTS audios on the client.
/// </summary>
public sealed class MsgPlayTts : NetMessage
{
    private const byte AudioBufferType = 0;
    private const byte SoundSpecifierType = 1;

    public List<PlayTtsMessageData> Datas = [];

    public override MsgGroups MsgGroup => MsgGroups.Command;
    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableUnordered;

    public MsgPlayTts() { }

    public MsgPlayTts(params PlayTtsMessageData[] datas)
    {
        Datas = [.. datas];
    }

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var count = buffer.ReadVariableInt32();
        Datas = new List<PlayTtsMessageData>(count);

        for (var i = 0; i < count; i++)
        {
            var type = buffer.ReadByte();
            var metadata = ReadMetadata(buffer);

            ITtsData data;
            switch (type)
            {
                case AudioBufferType:
                    var audio = new TtsAudioBufferData();
                    audio.ReadFromNetBuffer(buffer);
                    data = audio;
                    break;

                case SoundSpecifierType:
                    var streamLength = buffer.ReadVariableInt32();
                    using (var stream = new MemoryStream(buffer.ReadBytes(streamLength)))
                    {
                        data = new TtsSoundSpecifierData(serializer.Deserialize<SoundSpecifier>(stream));
                    }
                    break;

                default:
                    throw new InvalidDataException($"Unknown TTS data type {type}");
            }

            Datas.Add(new PlayTtsMessageData { TtsData = data, TtsMetadata = metadata });
        }
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        buffer.WriteVariableInt32(Datas.Count);

        foreach (var data in Datas)
        {
            switch (data.TtsData)
            {
                case TtsAudioBufferData audio:
                    buffer.Write(AudioBufferType);
                    WriteMetadata(buffer, data.TtsMetadata);
                    audio.WriteToNetBuffer(buffer);
                    break;

                case TtsSoundSpecifierData soundSpecifier:
                    buffer.Write(SoundSpecifierType);
                    WriteMetadata(buffer, data.TtsMetadata);

                    using (var stream = new MemoryStream())
                    {
                        serializer.Serialize(stream, soundSpecifier.SoundSpecifier);

                        var streamLength = (int)stream.Length;
                        buffer.WriteVariableInt32(streamLength);
                        buffer.Write(stream.GetBuffer().AsSpan(0, streamLength));
                    }
                    break;

                default:
                    throw new NotSupportedException($"Unsupported TTS data type {data.TtsData.GetType()}");
            }
        }
    }

    private static void WriteMetadata(NetOutgoingMessage buffer, TtsMetadata metadata)
    {
        buffer.Write((byte)metadata.Kind);

        buffer.Write(metadata.Provider.HasValue);
        if (metadata.Provider is { } provider)
            buffer.Write((byte)provider);

        buffer.Write(metadata.ChannelPrototype != null);
        if (metadata.ChannelPrototype != null)
            buffer.Write(metadata.ChannelPrototype);

        buffer.Write(metadata.Source ?? NetEntity.Invalid);
        buffer.Write(metadata.PlayEntity ?? NetEntity.Invalid);
    }

    private static TtsMetadata ReadMetadata(NetIncomingMessage buffer)
    {
        var metadata = new TtsMetadata
        {
            Kind = (TtsKind)buffer.ReadByte()
        };

        if (buffer.ReadBoolean())
            metadata.Provider = (TtsProvider)buffer.ReadByte();

        if (buffer.ReadBoolean())
            metadata.ChannelPrototype = buffer.ReadString();

        var source = buffer.ReadNetEntity();
        metadata.Source = source.Valid ? source : null;

        var playEntity = buffer.ReadNetEntity();
        metadata.PlayEntity = playEntity.Valid ? playEntity : null;

        return metadata;
    }
}
