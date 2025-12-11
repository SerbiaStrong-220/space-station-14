// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Signature;

[Serializable, NetSerializable]
public sealed class SignatureData
{
    public int Width;
    public int Height;
    public byte[] Pixels;

    public SignatureData(int w, int h)
    {
        Width = w;
        Height = h;
        Pixels = new byte[w * h];
    }

    public bool GetPixel(int x, int y)
    {
        return Pixels[y * Width + x] == 1;
    }

    public void SetPixel(int x, int y)
    {
        Pixels[y * Width + x] = 1;
    }

    public void ErasePixel(int x, int y)
    {
        Pixels[y * Width + x] = 0;
    }

    public void Clear()
    {
        Array.Fill(Pixels, (byte)0);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not SignatureData other)
            return false;

        if (Width != other.Width || Height != other.Height)
            return false;

        return Pixels.SequenceEqual(other.Pixels);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();

        hash.Add(Width);
        hash.Add(Height);

        foreach(var p in Pixels)
        {
            hash.Add(p);
        }

        return hash.ToHashCode();
    }

    public SignatureData Clone()
    {
        var copy = new SignatureData(Width, Height);
        Array.Copy(Pixels, copy.Pixels, Pixels.Length);
        return copy;
    }

    public void CopyTo(SignatureData clone)
    {
        var minWidth  = Math.Min(Width,  clone.Width);
        var minHeight = Math.Min(Height, clone.Height);

        for (var y = 0; y < minHeight; y++)
        {
            var originalOffset = y * Width;
            var cloneOffset = y * clone.Width;

            Array.Copy(
                Pixels,
                originalOffset,
                clone.Pixels,
                cloneOffset,
                minWidth
            );
        }
    }

    #region Serialization
    public string Serialize()
    {
        var sb = new StringBuilder(Width * Height + 16);
        sb.Append(Height);
        sb.Append('|');
        sb.Append(Width);
        sb.Append('|');

        for (var i = 0; i < Pixels.Length; i++)
        {
            sb.Append(Pixels[i]);
        }

        return sb.ToString();
    }

    public static SignatureData? Deserialize(string? data)
    {
        if (string.IsNullOrEmpty(data))
            return null;

        var parts = data.Split('|');
        if (parts.Length != 3)
            return null;

        if (!int.TryParse(parts[0], out var height))
            return null;

        if (!int.TryParse(parts[1], out var width))
            return null;

        var pixelsString = parts[2];

        var inst = new SignatureData(width, height);

        if (pixelsString.Length != inst.Pixels.Length)
            return null;

        for (var i = 0; i < pixelsString.Length; i++)
        {
            var c = pixelsString[i];
            inst.Pixels[i] = (byte)(c - '0'); // '1' == 49, '0' is 48
        }

        return inst;
    }
    #endregion Serialization
}

[Serializable]
public sealed class SignatureLogData(SignatureData data)
{
    [UsedImplicitly]
    public string Serialized { get; } = data.Serialize();

    public const string SignatureLogTag = "[Signature]";

    public override string ToString()
    {
        return $"Signature({data.Width}x{data.Height})";
    }
}

[Serializable, NetSerializable]
public sealed class SignatureSubmitMessage(SignatureData data) : BoundUserInterfaceMessage
{
    public SignatureData Data = data;
}

[Serializable, NetSerializable]
public sealed class ApplySavedSignature : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class UpdateSignatureDataState(SignatureData data) : BoundUserInterfaceState
{
    public SignatureData Data = data;
}

[Serializable, NetSerializable]
public sealed class UpdatePenBrushPaperState(int brushWriteSize, int brushEraseSize) : BoundUserInterfaceState
{
    public int BrushWriteSize = brushWriteSize;
    public int BrushEraseSize = brushEraseSize;
}

[Serializable, NetSerializable]
public sealed class RequestSignatureAdminMessage(int logId, DateTime time) : BoundUserInterfaceMessage
{
    public int LogId = logId;
    public DateTime Time = time;
}

[Serializable, NetSerializable]
public sealed class SendSignatureToAdminEvent(SignatureData data) : EntityEventArgs
{
    public SignatureData Data = data;
}
