// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Lidgren.Network;
using Robust.Shared.Audio;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.TTS;

public interface ITtsData;

public struct TtsAudioBufferData : ITtsData
{
    public byte[] Buffer = [];
    public int Length;

    public readonly bool IsEmpty => Length == 0;

    public TtsAudioBufferData(byte[] bytes, int length)
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

    /// <summary>
    /// Writes only the first <see cref="Length"/> bytes, so the buffer may be bigger (e.g. rented from the array pool).
    /// </summary>
    public readonly void WriteToNetBuffer(NetOutgoingMessage buffer)
    {
        buffer.Write(Length);
        buffer.Write(new ReadOnlySpan<byte>(Buffer, 0, Length));
    }
}

public struct TtsSoundSpecifierData(SoundSpecifier specifier) : ITtsData
{
    public SoundSpecifier SoundSpecifier = specifier;
}
