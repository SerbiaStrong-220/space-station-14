using Content.Shared.SS220.DarkForces.Ratvar.Structures.Altar;
using Robust.Client.Animations;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.DarkForces.Ratvar.Structures.Altar;

public sealed class AltarGlowSystem : EntitySystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;

    private const string AltarGlowAnimationKey = "ratvarAltarGlow";
    private const float RevealAlpha = 0.8f;
    private const double AnimationLength = 0.7;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RatvarAltarGlowComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, RatvarAltarGlowComponent component, ComponentInit args)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        _animation.Play(uid, new Animation
        {
            Length = TimeSpan.FromSeconds(AnimationLength),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Color),
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(sprite.Color.WithAlpha(0f), 0f),
                        new AnimationTrackProperty.KeyFrame(sprite.Color.WithAlpha(RevealAlpha),
                            (float) AnimationLength)
                    }
                }
            }
        }, AltarGlowAnimationKey);
    }
}
