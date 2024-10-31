// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Client.Resources;
using Content.Client.SS220.Fonts;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.SS220.StyleTools;

public abstract class QuickStyle
{
    protected StylesheetBuilder Builder { get; private set; } = default!;
    protected IResourceCache Resources { get; private set; } = default!;

    public Stylesheet Create(Stylesheet? baseStylesheet, IResourceCache resourceCache)
    {
        Builder = StylesheetBuilder.FromBase(baseStylesheet);
        Resources = resourceCache;
        CreateRules();
        return Builder.Build();
    }

    protected abstract void CreateRules();

    protected Texture Tex(string path) => Resources.GetTexture(path);
    protected Texture Tex(ResPath path) => Resources.GetTexture(path);
    protected SpriteSpecifier Sprite(string path, string state) => new SpriteSpecifier.Rsi(new(path), state);
    protected SpriteSpecifier Sprite(ResPath path, string state) => new SpriteSpecifier.Rsi(path, state);
    protected Font VectorFont(string path, int size) => Resources.GetFont(path, size);
    protected Font VectorFont(ResPath path, int size) => Resources.GetFont(path, size);
    protected SpriteFont SpriteFont(ProtoId<SpriteFontPrototype> id) => Fonts.SpriteFont.Load(id, Resources);

    protected StyleBoxTexture StrechedStyleBoxTexture(Texture texture)
    {
        var box = new StyleBoxTexture()
        {
            Texture = texture,
        };
        box.SetPatchMargin(StyleBox.Margin.All, 0);
        return box;
    }
}
