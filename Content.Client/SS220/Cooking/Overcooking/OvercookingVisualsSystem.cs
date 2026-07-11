// SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Cooking.Overcooking;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.Cooking.Overcooking;

/// <summary>
/// Handles client-side visuals for food that is in the process of overcooking.
/// </summary>
public sealed class OvercookingVisualsSystem : EntitySystem
{
    private static readonly Color BurntColor = Color.FromHex("#4a2b1f");

    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OvercookingComponent, AfterAutoHandleStateEvent>(OnOvercookingState);
    }

    private void OnOvercookingState(Entity<OvercookingComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        var burnProgress = GetBurnProgress(ent.Comp);
        _sprite.SetColor((ent.Owner, sprite), LerpColor(Color.White, BurntColor, burnProgress));
    }

    private static float GetBurnProgress(OvercookingComponent component)
    {
        var burnTime = component.TimeToOvercook - component.MinOvercookingTime;
        if (burnTime <= 0)
            return 1f;

        return Math.Clamp((component.CurrentOvercookTime - component.MinOvercookingTime) / burnTime, 0f, 1f);
    }

    private static Color LerpColor(Color from, Color to, float ratio)
    {
        return new Color(
            from.R + (to.R - from.R) * ratio,
            from.G + (to.G - from.G) * ratio,
            from.B + (to.B - from.B) * ratio,
            from.A + (to.A - from.A) * ratio);
    }
}
