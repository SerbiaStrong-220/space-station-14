// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Audio;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.TTS;

public interface ITtsData;

[Serializable, NetSerializable]
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
}

[Serializable, NetSerializable]
public struct TtsSoundSpecifierData(SoundSpecifier specifier) : ITtsData
{
    public SoundSpecifier SoundSpecifier = specifier;
}
