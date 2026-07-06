// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;

namespace Content.Client.SS220.Virology;

public sealed class DnaProgressBar : Control
{
    public float Progress;

    public Color StrandColor = Color.FromHex("#31843E");
    public Color StrandDimColor = Color.FromHex("#243A29");
    public Color RungColor = Color.FromHex("#A88B5E");
    public Color RungDimColor = Color.FromHex("#2C2C32");
    public Color BackgroundColor = Color.FromHex("#141418");

    public int Rungs = 14;

    public float Turns = 2.5f;

    public DnaProgressBar()
    {
        MinSize = new Vector2(0, 38);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        var w = PixelWidth;
        var h = PixelHeight;
        if (w <= 2f || h <= 2f)
            return;

        handle.DrawRect(PixelSizeBox, BackgroundColor);

        var progress = Math.Clamp(Progress, 0f, 1f);
        var frontier = progress * w;
        var midY = h / 2f;
        var amp = h * 0.32f;
        var scale = UIScale;

        for (var r = 0; r <= Rungs; r++)
        {
            var t = r / (float)Rungs;
            var x = t * w;
            var sin = MathF.Sin(t * Turns * MathF.Tau) * amp;
            handle.DrawLine(new Vector2(x, midY + sin), new Vector2(x, midY - sin), x <= frontier ? RungColor : RungDimColor);
        }

        var segs = Math.Max(32, (int)(w / (3f * scale)));
        Vector2 prevA = default, prevB = default;
        for (var i = 0; i <= segs; i++)
        {
            var t = i / (float)segs;
            var x = t * w;
            var sin = MathF.Sin(t * Turns * MathF.Tau) * amp;
            var a = new Vector2(x, midY + sin);
            var b = new Vector2(x, midY - sin);

            if (i > 0)
            {
                var col = x <= frontier ? StrandColor : StrandDimColor;
                DrawThick(handle, prevA, a, col, scale);
                DrawThick(handle, prevB, b, col, scale);
            }

            prevA = a;
            prevB = b;
        }

        for (var r = 0; r <= Rungs; r++)
        {
            var t = r / (float)Rungs;
            var x = t * w;
            var sin = MathF.Sin(t * Turns * MathF.Tau) * amp;
            var col = x <= frontier ? StrandColor : StrandDimColor;
            var nodeR = 2.5f * scale;
            handle.DrawCircle(new Vector2(x, midY + sin), nodeR, col);
            handle.DrawCircle(new Vector2(x, midY - sin), nodeR, col);
        }

        if (progress > 0f && progress < 1f)
            handle.DrawLine(new Vector2(frontier, 0f), new Vector2(frontier, h), StrandColor.WithAlpha(0.5f));
    }

    private static void DrawThick(DrawingHandleScreen handle, Vector2 from, Vector2 to, Color color, float scale)
    {
        handle.DrawLine(from, to, color);
        var off = new Vector2(0f, scale);
        handle.DrawLine(from + off, to + off, color);
    }
}
