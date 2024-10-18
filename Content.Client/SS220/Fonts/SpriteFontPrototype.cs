// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.SS220.Fonts;

[Prototype("spriteFont")]
public sealed class SpriteFontPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public ResPath Texture { get; private set; }

    [DataField(required: true)]
    public string GlyphMap { get; private set; } = default!;

    [DataField(required: true)]
    public Vector2i GridSize
    {
        get => _gridSize;
        private set
        {
            if (value.X < 1 || value.Y < 1)
            {
                throw new ArgumentException($"{nameof(GridSize)} can not be less than (1, 1)");
            }
            _gridSize = value;
        }
    }
    private Vector2i _gridSize;

    [DataField]
    public Vector2i GridOffset { get; private set; }

    [DataField]
    public Vector2i GridGap { get; private set; }

    [DataField]
    public Vector2i? SymbolSize { get; private set; }

    [DataField]
    public int? WhitespaceSize { get; private set; }

    [DataField]
    public float Scale { get; private set; } = 1f;

    [DataField]
    public float LineInterval { get; private set; } = 0f;

    [DataField]
    public Dictionary<string, SpriteFontPrototypeGlyph> Glyphs { get; private set; } = new();
}

[DataDefinition]
public partial struct SpriteFontPrototypeGlyph
{
    [DataField]
    public Vector2i? Size { get; private set; }

    [DataField]
    public Vector2i SizeAdjustment { get; private set; }

    [DataField]
    public Vector2i Offset { get; private set; }
}
