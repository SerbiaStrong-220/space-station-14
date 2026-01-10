// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.MartialArts.Effects;

public abstract partial class BaseMartialArtEffectSystem<TEffect> : EntitySystem where TEffect : MartialArtEffect
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
            if (sub is TEffect effect1)
            {
                effect = effect1;
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

public abstract partial class BaseMartialArtEffectSystem<TEffect, TComp> : BaseMartialArtEffectSystem<TEffect> where TEffect : MartialArtEffect where TComp : IComponent, new()
{
    [MustCallBase]
    protected override void StartupEffect(EntityUid user, MartialArtistComponent artist, TEffect effect)
    {
        DebugTools.Assert(!HasComp<TComp>(user), $"On startup of effect \"{typeof(TEffect)}\" the component \"{typeof(TComp)}\" already was assigned");
        EnsureComp<TComp>(user);
    }

    [MustCallBase]
    protected override void ShutdownEffect(EntityUid user, MartialArtistComponent artist, TEffect effect)
    {
        if (!TryComp<TComp>(user, out var comp))
        {
            DebugTools.Assert($"On shutdown of effect \"{typeof(TEffect)}\" the component \"{typeof(TComp)}\" is missing");
            return;
        }

        RemCompDeferred(user, comp);
    }
}

public abstract partial class MartialArtEffectBase<T> : MartialArtEffect where T : MartialArtEffectBase<T>
{
    public override void RaiseStartupEvent(EntityUid user, IMartialArtEffectEventRaiser raiser)
    {
        if (this is not T type)
            return;

        raiser.RaiseMartialEffectStartup(user, type);
    }

    public override void RaiseShutdownEvent(EntityUid user, IMartialArtEffectEventRaiser raiser)
    {
        if (this is not T type)
            return;

        raiser.RaiseMartialEffectShutdown(user, type);
    }
}

public interface IMartialArtEffectEventRaiser
{
    void RaiseMartialEffectStartup<T>(EntityUid user, T effect) where T : MartialArtEffect;
    void RaiseMartialEffectShutdown<T>(EntityUid user, T effect) where T : MartialArtEffect;
}

[ImplicitDataDefinitionForInheritors]
public abstract partial class MartialArtEffect
{
    public abstract void RaiseStartupEvent(EntityUid user, IMartialArtEffectEventRaiser raiser);
    public abstract void RaiseShutdownEvent(EntityUid user, IMartialArtEffectEventRaiser raiser);

    // needed to enforce that effects won't be duplicated in lists even ignoring their properties
    // effects mostly will break with duplicates
    public bool Equals<T>(T effect) where T : MartialArtEffect
    {
        return typeof(T) == GetType();
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj)) return true;
        return obj is MartialArtEffect effect && Equals(effect);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return GetType().GetHashCode();
    }
}

public sealed partial class MartialArtEffectStartupEvent<TEffect> : EntityEventArgs where TEffect : MartialArtEffect
{
    public TEffect Effect;

    public MartialArtEffectStartupEvent(TEffect effect)
    {
        Effect = effect;
    }
}

public sealed partial class MartialArtEffectShutdownEvent<TEffect> : EntityEventArgs where TEffect : MartialArtEffect
{
    public TEffect Effect;

    public MartialArtEffectShutdownEvent(TEffect effect)
    {
        Effect = effect;
    }
}
