// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Lidgren.Network;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.TTS;

public sealed class MsgPlayTts : NetMessage
{
    public static readonly int NullValue = -1;

    public TtsAudioData Data { get; set; }
    public NetEntity? SourceUid { get; set; }
    public TtsMetadata Metadata { get; set; }
    public float VolumeModifier { get; set; } = 1f;

    public override MsgGroups MsgGroup => MsgGroups.Command;
    public override NetDeliveryMethod DeliveryMethod => NetDeliveryMethod.ReliableUnordered;

    public void SetVolumeModifier(float modifier)
    {
        VolumeModifier = Math.Clamp(modifier, 0, 3);
    }

    public override void ReadFromBuffer(NetIncomingMessage buffer, IRobustSerializer serializer)
    {
        var data = new TtsAudioData();
        data.ReadFromNetBuffer(buffer);
        Data = data;
        SourceUid = buffer.ReadNetEntity();
        if (SourceUid is { Valid: false })
            SourceUid = null;
        var kind = (TtsKind)buffer.ReadInt32();
        var subkind = buffer.ReadString();
        VolumeModifier = buffer.ReadFloat();

        Metadata = new TtsMetadata(kind, subkind);
    }

    public override void WriteToBuffer(NetOutgoingMessage buffer, IRobustSerializer serializer)
    {
        Data.WriteToNetBuffer(buffer);
        buffer.Write(SourceUid ?? NetEntity.Invalid);
        buffer.Write((int)Metadata.Kind);
        buffer.Write(Metadata.Subkind);
        buffer.Write(VolumeModifier);
    }
}
