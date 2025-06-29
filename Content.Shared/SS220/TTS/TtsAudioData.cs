// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Lidgren.Network;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.TTS;

public struct TtsAudioData
{
    public byte[] Buffer = Array.Empty<byte>();
    public int Length;

    public readonly bool IsEmpty => Length == 0;

    public TtsAudioData(byte[] bytes, int length)
    {
        Buffer = bytes;
        Length = length;
        DebugTools.Assert(Length <= Buffer.Length);
    }

    public void ReadFromNetBuffer(NetIncomingMessage buffer)
    {
        Length = buffer.ReadInt32();
        Buffer = buffer.ReadBytes(Length);
    }

    public void WriteToNetBuffer(NetOutgoingMessage buffer)
    {
        buffer.Write(Length);
        buffer.Write(new ReadOnlySpan<byte>(Buffer, 0, Length));
    }

    public Memory<byte> AsMemory() => new(Buffer, 0, Length);
}
