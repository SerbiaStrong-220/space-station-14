// Original code licensed under Imperial CLA.
// Copyright holders: orix0689 (discord) and pocchitsu (discord)
// See Content.Shared/SS220/Ninja/Objectives/LeaveNoTrace/license.txt

// Modified and/or redistributed under SS220 CLA with hosting restrictions.
// Full license text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Numerics;
using Content.Client.Resources;
using Content.Shared.SS220.Ninja.Objectives.LeaveNoTrace.DataDefinition;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.Ninja.Objectives.LeaveNoTrace;

public sealed class RevealOverlay : Overlay
{
    [Dependency] private readonly IClyde _clyde = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;
    public override bool RequestScreenTexture => false;

    private const string FontPath = "/Fonts/SS220/Tuffy/Tuffy-Bold.ttf";
    private const int FontSize = 22;
    private float _scale;

    private readonly Font _font;
    private readonly ShaderInstance _fillShader;
    private readonly ShaderInstance _glitchShader;

    private IRenderTexture _buffer = default!;
    private TextureGlitchParametersData _textureGlitchParametersData = new();

    public float RevealProgress = 0.0f;
    public float FadeProgress = 0.0f;
    public string RevealLetter = "ВОБЛЯ";
    public TextureGlitchParametersData TextureParams
    {
        get => _textureGlitchParametersData;
        set
        {
            _textureGlitchParametersData = value;
            _buffer = GetBufferFromTextureParams(value);
        }
    }
    public GlitchShaderParametersData TextGlitchEffectParams = new();

    public bool IsReveal = false;

    public RevealOverlay()
    {
        IoCManager.InjectDependencies(this);

        _fillShader = _prototypeManager.Index<ShaderPrototype>("SpriteFill").InstanceUnique();
        _glitchShader = _prototypeManager.Index<ShaderPrototype>("Glitch").InstanceUnique();
        _font = _resourceCache.GetFont(FontPath, FontSize);

        _configurationManager.OnValueChanged(CVars.DisplayUIScale, v => _scale = v == 0 ? 1 : v, true);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var screen = args.ScreenHandle;

        var scaleMatrix = Matrix3Helpers.CreateScale(new Vector2(_scale));
        var matrix = Matrix3x2.Multiply(Matrix3x2.Identity, scaleMatrix);

        screen.SetTransform(matrix);

        if (IsReveal)
            DrawRevealText(args);
        else
            DrawRevealSprite(args);

        screen.SetTransform(Matrix3x2.Identity);
        screen.UseShader(null);
    }

    #region Helpers

    private void DrawRevealText(OverlayDrawArgs args)
    {
        var screen = args.ScreenHandle;

        var viewportBounds = args.ViewportBounds.BottomRight + args.ViewportBounds.TopLeft;
        var textWidth = RevealLetter.Length * FontSize;
        var textPosition = new Vector2(
            viewportBounds.X / 2.0f - textWidth / 2.0f,
            viewportBounds.Y * 0.1f
        ) / _scale;

        SetGlitchShaderParameters(TextGlitchEffectParams);

        screen.UseShader(_glitchShader);
        screen.DrawString(_font, textPosition, RevealLetter, Color.White.WithAlpha(1.0f - FadeProgress));
    }

    private void DrawRevealSprite(OverlayDrawArgs args)
    {
        var screen = args.ScreenHandle;

        var viewportBounds = args.ViewportBounds.BottomRight + args.ViewportBounds.TopLeft;
        var spriteTexture = _resourceCache.GetTexture(TextureParams.RevealSpritePath);
        var texturePosition = new Vector2(
            viewportBounds.X / 2.0f - spriteTexture.Size.X / 2.0f,
            viewportBounds.Y * 0.05f
        ) / _scale;

        var progress = Math.Clamp(RevealProgress, 0.0f, 1.0f);

        _fillShader?.SetParameter("progress", progress);
        SetGlitchShaderParameters(TextureParams.Glitch);

        screen.RenderInRenderTarget(_buffer, () =>
        {
            if (RevealProgress >= TextureParams.GlitchThreshold)
                screen.UseShader(_glitchShader);

            screen.DrawTextureRect(spriteTexture, new UIBox2(Vector2.Zero, _buffer.Size));

            screen.UseShader(null);
        }, Color.Transparent);

        screen.UseShader(_fillShader);
        screen.DrawTexture(_buffer.Texture, texturePosition);
    }

    private void SetGlitchShaderParameters(GlitchShaderParametersData data)
    {
        _glitchShader?.SetParameter("shake_power", data.ShakePower);
        _glitchShader?.SetParameter("shake_rate", data.SnakeRate);
        _glitchShader?.SetParameter("shake_speed", data.SnakeSpeed);
        _glitchShader?.SetParameter("shake_block_size", data.ShakeBlockSize);
        _glitchShader?.SetParameter("shake_color_rate", data.SnakeColorRate);
    }

    private IRenderTexture GetBufferFromTextureParams(TextureGlitchParametersData param)
    {
        var texture = _resourceCache.GetTexture(TextureParams.RevealSpritePath);

        return _clyde.CreateRenderTarget(
            texture.Size,
            new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb, true),
            new TextureSampleParameters { Filter = true },
            nameof(RevealOverlay)
        );
    }

    #endregion
}
