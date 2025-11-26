using System.Linq;
using System.Text;
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

    public void SetPixel(int x, int y, bool erase = false)
    {
        Pixels[y * Width + x] = erase ? (byte)0 : (byte)1;
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
            sb.Append(Pixels[i] == 1 ? '1' : '0');
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
            inst.Pixels[i] = c switch
            {
                '1' => 1,
                '0' => 0,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        return inst;
    }
    #endregion Serialization
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
