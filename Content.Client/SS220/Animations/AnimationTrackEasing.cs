// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Maths;
using Robust.Client.Animations;

namespace Content.Client.SS220.Animations;

public sealed class AnimationTrackEasing : AnimationTrack
{
    public Easing Easing { get; set; }
    public TimeSpan Duration { get; set; }
    public Func<float, float>? CustomEasingFunc { get; set; }
    public AnimationTrack? Track { get; set; }

    public override (int KeyFrameIndex, float FramePlayingTime) InitPlayback()
    {
        return Track?.InitPlayback() ?? default;
    }

    public override (int KeyFrameIndex, float FramePlayingTime) AdvancePlayback(object context, int prevKeyFrameIndex, float prevPlayingTime, float frameTime)
    {
        var durationSeconds = (float)Duration.TotalSeconds;
        var playingTime = prevPlayingTime + frameTime;
        var normalizedPlayingTime = playingTime / durationSeconds;
        var easedFrameTime = frameTime;
        if (normalizedPlayingTime < 1f)
        {
            normalizedPlayingTime = EasingsExtensions.Ease(Easing, normalizedPlayingTime, CustomEasingFunc);
            var easedPlayingTime = normalizedPlayingTime * durationSeconds;
            easedFrameTime = easedPlayingTime - prevPlayingTime;
        }
        var (frame, _) = Track?.AdvancePlayback(context, prevKeyFrameIndex, prevPlayingTime, easedFrameTime) ?? default;
        return (frame, playingTime);
    }
}
