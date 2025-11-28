// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.SS220.MartialArts.Effects;

public abstract partial class BaseMartialArtEffectSystem<TEffect> : EntitySystem where TEffect : BaseMartialArtEffect
{
    [Dependency] protected readonly MartialArtsSystem MartialArts = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MartialArtistComponent, MartialArtEffectStartupEvent<TEffect>>(OnStartupEvent);
        SubscribeLocalEvent<MartialArtistComponent, MartialArtEffectShutdownEvent<TEffect>>(OnShutdownEvent);
    }

    protected bool HasEffect(EntityUid uid, MartialArtistComponent? artist = null)
    {
        return TryEffect(uid, out _, artist);
    }

    protected bool TryEffect(EntityUid uid, [NotNullWhen(true)] out TEffect? effect, MartialArtistComponent? artist = null)
    {
        effect = null;

        if (!Resolve(uid, ref artist))
            return false;

        var effects = MartialArts.GetMartialArtEffects(uid, artist);

        foreach (var sub in effects)
        {
            if (sub is TEffect)
            {
                effect = (TEffect)sub;
                return true;
            }
        }

        return false;
    }

    private void OnStartupEvent(EntityUid uid, MartialArtistComponent comp, MartialArtEffectStartupEvent<TEffect> ev)
    {
        StartupEffect(uid, comp, ev.Effect);
    }

    private void OnShutdownEvent(EntityUid uid, MartialArtistComponent comp, MartialArtEffectShutdownEvent<TEffect> ev)
    {
        ShutdownEffect(uid, comp, ev.Effect);
    }

    protected virtual void StartupEffect(EntityUid user, MartialArtistComponent artist, TEffect effect)
    {
    }

    protected virtual void ShutdownEffect(EntityUid user, MartialArtistComponent artist, TEffect effect)
    {
    }
}

[ImplicitDataDefinitionForInheritors]
public abstract partial class BaseMartialArtEffect;

public sealed partial class MartialArtEffectStartupEvent<TEffect> : EntityEventArgs where TEffect : BaseMartialArtEffect
{
    public TEffect Effect;

    public MartialArtEffectStartupEvent(TEffect effect)
    {
        Effect = effect;
    }
}

public sealed partial class MartialArtEffectShutdownEvent<TEffect> : EntityEventArgs where TEffect : BaseMartialArtEffect
{
    public TEffect Effect;

    public MartialArtEffectShutdownEvent(TEffect effect)
    {
        Effect = effect;
    }
}
