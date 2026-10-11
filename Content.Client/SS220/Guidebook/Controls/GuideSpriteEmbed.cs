// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using Content.Client.Guidebook.Richtext;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.SS220.Guidebook.Controls;

/// <summary>
/// Displays an RSI icon in a guide without spawning an entity.
/// </summary>
[UsedImplicitly]
public sealed class GuideSpriteEmbed : TextureRect, IDocumentTag
{
    private const float DefaultScale = 2f;

    [Dependency] private readonly IEntitySystemManager _systemManager = default!;

    /// <summary>
    /// Creates a centered sprite display with padding around the image.
    /// </summary>
    public GuideSpriteEmbed()
    {
        IoCManager.InjectDependencies(this);
        Stretch = StretchMode.KeepCentered;
        Margin = new Thickness(8);
    }

    /// <summary>
    /// Configures the embed to display the first frame of the requested RSI state.
    /// </summary>
    /// <param name="args"> Tag attributes: required <c>Sprite</c> RSI path and <c>State</c> name, and optional <c>Scale</c>.</param>
    /// <param name="control">This control on success; otherwise <see langword="null"/>.</param>
    /// <returns>
    /// <see langword="false"/> if a required attribute is missing or the scale is invalid;
    /// otherwise <see langword="true"/> after configuring the image.
    /// </returns>
    public bool TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
    {
        control = null;
        if (!args.TryGetValue("Sprite", out var sprite))
            return false;

        if (!args.TryGetValue("State", out var state))
            return false;

        if (!TryGetScale(args, out var scale))
            return false;

        Texture = _systemManager.GetEntitySystem<SpriteSystem>().Frame0(new SpriteSpecifier.Rsi(new ResPath(sprite), state));
        TextureScale = new Vector2(scale);
        control = this;
        return true;
    }

    /// <summary>
    /// Reads a positive, finite scale using invariant culture, or uses <see cref="DefaultScale"/> when omitted.
    /// </summary>
    /// <param name="args">Tag attributes that may contain <c>Scale</c>.</param>
    /// <param name="scale">The parsed or default scale on success; unspecified on failure.</param>
    /// <returns><see langword="true"/> if the scale is omitted or valid; otherwise <see langword="false"/>.</returns>
    private static bool TryGetScale(Dictionary<string, string> args, out float scale)
    {
        scale = DefaultScale;
        if (!args.TryGetValue("Scale", out var value))
            return true;

        if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out scale))
            return false;

        if (!float.IsFinite(scale))
            return false;

        return scale > 0;
    }
}
