// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
namespace Content.Shared.SS220.Maths;

/// <summary>
/// See <see cref="Easings"/> and <see href="https://easings.net/"/>
/// </summary>
public enum Easing
{
    Linear, // Basically none
    Custom, // User custom function
    InSine,
    OutSine,
    InOutSine,
    InQuad,
    OutQuad,
    InOutQuad,
    InCubic,
    OutCubic,
    InOutCubic,
    InQuart,
    OutQuart,
    InOutQuart,
    InQuint,
    OutQuint,
    InOutQuint,
    InExpo,
    OutExpo,
    InOutExpo,
    InCirc,
    OutCirc,
    InOutCirc,
    InBack,
    OutBack,
    InOutBack,
    InElastic,
    OutElastic,
    InOutElastic,
    InBounce,
    OutBounce,
    InOutBounce,
}

public static class EasingsExtensions
{
    public static float Ease(Easing easing, float p, Func<float, float>? customFunction = null)
    {
        return easing switch
        {
            Easing.Linear => p,
            Easing.Custom => customFunction?.Invoke(p) ?? p,
            Easing.InSine => Easings.InSine(p),
            Easing.OutSine => Easings.OutSine(p),
            Easing.InOutSine => Easings.InOutSine(p),
            Easing.InQuad => Easings.InQuad(p),
            Easing.OutQuad => Easings.OutQuad(p),
            Easing.InOutQuad => Easings.InOutQuad(p),
            Easing.InCubic => Easings.InCubic(p),
            Easing.OutCubic => Easings.OutCubic(p),
            Easing.InOutCubic => Easings.InOutCubic(p),
            Easing.InQuart => Easings.InQuart(p),
            Easing.OutQuart => Easings.OutQuart(p),
            Easing.InOutQuart => Easings.InOutQuart(p),
            Easing.InQuint => Easings.InQuint(p),
            Easing.OutQuint => Easings.OutQuint(p),
            Easing.InOutQuint => Easings.InOutQuint(p),
            Easing.InExpo => Easings.InExpo(p),
            Easing.OutExpo => Easings.OutExpo(p),
            Easing.InOutExpo => Easings.InOutExpo(p),
            Easing.InCirc => Easings.InCirc(p),
            Easing.OutCirc => Easings.OutCirc(p),
            Easing.InOutCirc => Easings.InOutCirc(p),
            Easing.InBack => Easings.InBack(p),
            Easing.OutBack => Easings.OutBack(p),
            Easing.InOutBack => Easings.InOutBack(p),
            Easing.InElastic => Easings.InElastic(p),
            Easing.OutElastic => Easings.OutElastic(p),
            Easing.InOutElastic => Easings.InOutElastic(p),
            Easing.InBounce => Easings.InBounce(p),
            Easing.OutBounce => Easings.OutBounce(p),
            Easing.InOutBounce => Easings.InOutBounce(p),
            _ => p,
        };
    }
}
