// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Utility;

namespace Content.Shared.SS220.TTS;

public struct TtsAudioData
{
    public byte[] Buffer = [];
    public int RentedLength;

    public readonly bool IsEmpty => RentedLength == 0;

    public TtsAudioData(byte[] bytes, int rentedLength)
    {
        Buffer = bytes;
        RentedLength = rentedLength;
        DebugTools.Assert(RentedLength <= Buffer.Length);
    }
}
