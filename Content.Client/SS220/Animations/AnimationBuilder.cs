// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Linq.Expressions;
using Content.Shared.SS220.Maths;
using Robust.Client.Animations;
using Robust.Shared.Animations;

namespace Content.Client.SS220.Animations;

// It is pain to write animations, so...
public readonly struct AnimationBuilder
{
    private readonly Animation _animation = new();

    public AnimationBuilder(float lengthSeconds) : this(TimeSpan.FromSeconds(lengthSeconds)) { }

    public AnimationBuilder(TimeSpan length)
    {
        _animation = new Animation()
        {
            Length = length,
        };
    }

    public AnimationBuilder WithTrack(AnimationTrack track)
    {
        _animation.AnimationTracks.Add(track);
        return this;
    }

    public AnimationBuilder WithControlPropertyTrack<TProperty>
        (string propertyName,
        Func<AnimationTrackControlPropertyBuilder<TProperty>, AnimationTrackControlPropertyBuilder<TProperty>> trackBuilder)
        where TProperty : notnull
    {
        return WithTrack(trackBuilder(new(propertyName, _animation.Length)).Build());
    }

    public Animation Build()
    {
        return _animation;
    }
}

public struct AnimationTrackControlPropertyBuilder<T> where T : notnull
{
    private AnimationTrackEasing? _easingTrack;
    private AnimationTiming _easingDuration = AnimationTiming.End;
    private readonly AnimationTrackControlProperty _track = new();
    private readonly TimeSpan _parentDuration;

    public AnimationTrackControlPropertyBuilder(string property, TimeSpan parentDuration)
    {
        _track.Property = property;
        _parentDuration = parentDuration;
    }

    public AnimationTrackControlPropertyBuilder<T> WithEasing(Easing easing, AnimationTiming? durationOverride = null)
    {
        _easingTrack = new()
        {
            Easing = easing,
        };
        if (durationOverride is { } duration)
        {
            _easingDuration = duration;
        }
        return this;
    }

    public AnimationTrackControlPropertyBuilder<T> WithEasing(Func<float, float> easeFunction, AnimationTiming? durationOverride = null)
    {
        _easingTrack = new()
        {
            Easing = Easing.Custom,
            CustomEasingFunc = easeFunction,
        };
        if (durationOverride is { } duration)
        {
            _easingDuration = duration;
        }
        return this;
    }

    public AnimationTrackControlPropertyBuilder<T> WithInterpolation(AnimationInterpolationMode interpolation)
    {
        _track.InterpolationMode = interpolation;
        return this;
    }

    public AnimationTrackControlPropertyBuilder<T> AppendKeyFrame(T value, AnimationTiming timing)
    {
        _track.KeyFrames.Add(new(value, timing.ToAbsoluteSeconds(_parentDuration)));
        return this;
    }

    public AnimationTrackControlPropertyBuilder<T> From(T value)
    {
        return AppendKeyFrame(value, AnimationTiming.Begin);
    }

    public AnimationTrackControlPropertyBuilder<T> To(T value)
    {
        return AppendKeyFrame(value, AnimationTiming.End);
    }

    public readonly AnimationTrack Build()
    {
        if (_easingTrack is { })
        {
            _easingTrack.Track = _track;
            _easingTrack.Duration = _easingDuration.ToAbsolute(_parentDuration);
            return _easingTrack;
        }
        else
        {
            return _track;
        }
    }
}

public readonly struct AnimationTiming
{
    public static AnimationTiming Begin => Relative(0f);
    public static AnimationTiming End => Relative(1f);

    private readonly float _value;
    private readonly bool _isRelative;

    private AnimationTiming(float value, bool isRelative)
    {
        _value = value;
        _isRelative = isRelative;
    }

    public static AnimationTiming Absolute(TimeSpan time)
    {
        return new((float)time.TotalSeconds, false);
    }

    public static AnimationTiming Seconds(float seconds)
    {
        return new(seconds, false);
    }

    public static AnimationTiming Relative(float normalizedTime)
    {
        return new(normalizedTime, true);
    }

    public TimeSpan ToAbsolute(TimeSpan duration)
    {
        return TimeSpan.FromSeconds(_isRelative ? (float)duration.TotalSeconds * _value : _value);
    }

    public float ToAbsoluteSeconds(TimeSpan duration)
    {
        return (float)ToAbsolute(duration).TotalSeconds;
    }

    public override string ToString()
    {
        return _isRelative ? _value.ToString("P0") : TimeSpan.FromSeconds(_value).ToString();
    }
}
