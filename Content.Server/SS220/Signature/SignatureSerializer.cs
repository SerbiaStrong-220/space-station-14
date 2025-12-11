// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Signature;

namespace Content.Server.SS220.Signature;

public static class SignatureSerializer
{
    public static readonly int BytePerEntry = 1;

    private const int MaxDimension = 1024;

    public static string Serialize(SignatureData data)
    {
        if (data.Width <= 0 || data.Height <= 0 || data.Width > MaxDimension || data.Height > MaxDimension)
            return string.Empty;

        var totalBytes = 8 + data.Pixels.Length * BytePerEntry;
        var buffer = new byte[totalBytes];

        BitConverter.TryWriteBytes(buffer.AsSpan(0, 4), data.Width);
        BitConverter.TryWriteBytes(buffer.AsSpan(4, 4), data.Height);

        Buffer.BlockCopy(data.Pixels, 0, buffer, 8, data.Pixels.Length * BytePerEntry);

        return Convert.ToBase64String(buffer);
    }

    public static SignatureData? Deserialize(string? data)
    {
        if (string.IsNullOrEmpty(data)) return null;
        try
        {
            var buffer = Convert.FromBase64String(data);
            if (buffer.Length < 8) return null;

            var width = BitConverter.ToInt32(buffer, 0);
            var height = BitConverter.ToInt32(buffer, 4);

            if (width <= 0 || height <= 0 || width > MaxDimension || height > MaxDimension)
                return null;

            var expectedSize = width * height * BytePerEntry;

            if (buffer.Length - 8 != expectedSize) return null;

            var instance = new SignatureData(width, height);
            Buffer.BlockCopy(buffer, 8, instance.Pixels, 0, expectedSize);

            return instance;
        }
        catch { return null; }
    }
}
