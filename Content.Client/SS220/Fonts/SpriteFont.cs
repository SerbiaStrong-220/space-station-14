// EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Numerics;
using System.Text;
using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.Fonts;

/// <summary>
///     Implemenation of sprite font (aka bitmap font).
/// </summary>
/// <remarks>
/// <para>
///     In contrast to <see cref="VectorFont"/>, <see cref="SpriteFont"/> renders glyphs from
///     provided texture atlas, which allows to create multi-colored fonts and stuff.
/// </para>
/// <para>
///     Use <see cref="Load(ProtoId{SpriteFontPrototype}, IResourceCache?)"/>
///     or <see cref="Load(SpriteFontPrototype, IResourceCache, ISawmill)"/>
///     in order to create instance.
/// </para>
/// </remarks>
public sealed class SpriteFont : Font
{
    public Vector2i DefaultGlyphSize { get; private set; }
    public int WhitespaceSize { get; private set; } = 1;
    public float Scale { get; private set; } = 1f;
    public float LineInterval { get; private set; } = 0f;

    private readonly Dictionary<Rune, Glyph> _glyphs = new();

    public override int GetAscent(float scale)
    {
        return GetHeight(scale);
    }

    public override int GetHeight(float scale)
    {
        return (int)(DefaultGlyphSize.Y * scale * Scale);
    }

    public override int GetDescent(float scale)
    {
        return 0;
    }

    public override int GetLineHeight(float scale)
    {
        return GetHeight(scale) + (int)(LineInterval * scale * Scale);
    }

    public override float DrawChar(DrawingHandleScreen handle, Rune rune, Vector2 baseline, float scale, Color color, bool fallback = true)
    {
        var totalScale = scale * Scale;
        if (Rune.IsWhiteSpace(rune))
        {
            return WhitespaceSize * totalScale;
        }
        if (GetCharMetricsInternal(rune, scale, false) is not (var metrics, var glyph))
        {
            if (!fallback)
            {
                return 0;
            }
            return DrawChar(handle, new Rune('�'), baseline, scale, color, false);
        }
        baseline.X += metrics.BearingX;
        baseline.Y -= metrics.BearingY;
        var rect = new UIBox2(baseline, baseline + glyph.Texture.Size * totalScale);
        handle.DrawTextureRect(glyph.Texture, rect, color);
        return metrics.Width;
    }

    public override CharMetrics? GetCharMetrics(Rune rune, float scale, bool fallback = true)
    {
        return GetCharMetricsInternal(rune, scale, fallback)?.metrics;
    }

    public static SpriteFont Load(ProtoId<SpriteFontPrototype> id, IResourceCache? resourceCache = null)
    {
        var prototype = IoCManager.Resolve<IPrototypeManager>().Index(id);
        resourceCache ??= IoCManager.Resolve<IResourceCache>();
        var sawmill = IoCManager.Resolve<ILogManager>().GetSawmill(nameof(SpriteFont));
        return Load(prototype, resourceCache, sawmill);
    }

    public static SpriteFont Load(SpriteFontPrototype prototype, IResourceCache resourceCache, ISawmill sawmill)
    {
        var font = new SpriteFont()
        {
            DefaultGlyphSize = prototype.SymbolSize ?? prototype.GridSize,
            Scale = prototype.Scale,
            LineInterval = prototype.LineInterval,
        };
        font.WhitespaceSize = prototype.WhitespaceSize ?? font.DefaultGlyphSize.X;
        var fontTexture = resourceCache.GetTexture(prototype.Texture);
        var gridSize = prototype.GridSize;
        var gridOffset = prototype.GridOffset;
        var gridGap = prototype.GridGap;
        var columns = (fontTexture.Size.X - gridOffset.X) / (gridSize.X + gridGap.X);
        // Attempt to add last column that will be without subsequent gap
        if ((gridSize.X + gridGap.X) * columns + gridOffset.X + gridSize.X <= fontTexture.Size.X)
        {
            columns++;
        }
        if (columns <= 0)
        {
            sawmill.Error($"[{prototype.ID}] SpriteFont grid width is greater than texture width");
            return font;
        }
        var i = -1;
        foreach (var rune in prototype.GlyphMap.EnumerateRunes())
        {
            if (Rune.IsControl(rune) || Rune.IsWhiteSpace(rune))
            {
                continue;
            }
            i++;
            var texCoords = new Vector2(
                i % columns * (gridSize.X + gridGap.X) + gridOffset.X,
                i / columns * (gridSize.Y + gridGap.Y) + gridOffset.Y);
            var box = new UIBox2(texCoords, texCoords + gridSize);
            if (box.Bottom >= fontTexture.Height)
            {
                sawmill.Error($"[{prototype.ID}] SpriteFont glyph is out of range on a texture");
                break;
            }
            var tex = new AtlasTexture(fontTexture, box);
            var glyph = new Glyph(tex, rune);
            if (!font._glyphs.TryAdd(rune, glyph))
            {
                sawmill.Error($"[{prototype.ID}] Mapped multiple '{rune}' glyphs, this is not supported");
            }
        }
        foreach (var (str, glyphProto) in prototype.Glyphs)
        {
            if (GetSingleRuneOrDefaut(str) is not { } rune)
            {
                sawmill.Error($"[{prototype.ID}] Provided '{str}' glyth key must be a single character");
                continue;
            }
            if (!font._glyphs.TryGetValue(rune, out var glyph))
            {
                sawmill.Error($"[{prototype.ID}] Glyph '{rune}' is not mapped");
                continue;
            }
            glyph.Size = glyphProto.Size + glyphProto.SizeAdjustment;
            glyph.Offset = glyphProto.Offset;
            font._glyphs[rune] = glyph;
        }
        return font;
    }

    private (CharMetrics metrics, Glyph glyph)? GetCharMetricsInternal(Rune rune, float scale, bool fallback)
    {
        scale *= Scale;
        if (!_glyphs.TryGetValue(rune, out var glyph))
        {
            if (!fallback)
            {
                return null;
            }
            return GetCharMetricsInternal(new Rune('�'), scale, false);
        }
        var size = glyph.Size ?? DefaultGlyphSize;
        var sizeScaled = (Vector2i)(size * scale);
        var offset = (Vector2i)(glyph.Offset * scale);
        var metrics = new CharMetrics(offset.X, sizeScaled.Y + offset.Y, sizeScaled.X, sizeScaled.X, sizeScaled.Y);
        return (metrics, glyph);
    }

    private static Rune? GetSingleRuneOrDefaut(string str)
    {
        var runesEnumerator = str.EnumerateRunes();
        if (!runesEnumerator.MoveNext())
        {
            return null;
        }
        var rune = runesEnumerator.Current;
        if (runesEnumerator.MoveNext())
        {
            return null;
        }
        return rune;
    }

    private struct Glyph(Texture texture, Rune rune)
    {
        public Texture Texture { get; } = texture;
        public Rune Rune { get; } = rune;
        public Vector2i? Size { get; set; }
        public Vector2i Offset { get; set; }
    }
}
