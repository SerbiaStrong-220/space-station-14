// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Maths;

namespace Content.Client.SS220.Forensics;

/// <summary>
/// Draws a simple printer outline whose color indicates whether the analyzer has a linked copier.
/// </summary>
public sealed class MicroFiberAnalyzerPrinterIcon : Control
{
    private const float IconSize = 24f;

    private static readonly UIBox2[] Sections =
    [
        // Paper entering the printer.
        new(6, 1, 18, 3),
        new(6, 3, 8, 7),
        new(16, 3, 18, 7),
        // Printer body and indicator.
        new(2, 7, 22, 9),
        new(2, 9, 4, 18),
        new(20, 9, 22, 18),
        new(4, 16, 6, 18),
        new(18, 16, 20, 18),
        new(16, 10, 18, 12),
        // Printed sheet and text line.
        new(6, 13, 18, 15),
        new(6, 15, 8, 23),
        new(16, 15, 18, 23),
        new(8, 21, 16, 23),
        new(10, 17, 14, 19),
    ];

    /// <inheritdoc/>
    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (PixelSize.X <= 0 || PixelSize.Y <= 0)
            return;

        var scale = Math.Min(PixelSize.X, PixelSize.Y) / IconSize;
        var offset = new Vector2((PixelSize.X - IconSize * scale) / 2f, (PixelSize.Y - IconSize * scale) / 2f);
        foreach (var section in Sections)
        {
            var bounds = new UIBox2(offset + section.TopLeft * scale, offset + section.BottomRight * scale);
            handle.DrawRect(bounds, Color.White);
        }
    }
}
