// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.MartialArts.Effects;

namespace Content.Shared.SS220.MartialArts;

public sealed partial class MartialArtsSystem
{
    private void OnShutdown(EntityUid user, MartialArtistComponent artist, ComponentShutdown ev)
    {
        if (!_prototype.TryIndex(artist.MartialArt, out var martialArt))
            return;

        ShutdownEffects(user, martialArt);
    }

    private void StartupEffects(EntityUid user, MartialArtPrototype martialArt)
    {
        foreach (var effect in martialArt.Effects)
        {
            effect.RaiseStartupEvent(user, this);
        }
    }

    private void ShutdownEffects(EntityUid user, MartialArtPrototype martialArt)
    {
        foreach (var effect in martialArt.Effects)
        {
            effect.RaiseShutdownEvent(user, this);
        }
    }

    /// <summary>
    /// Should be never called outside of this system
    /// </summary>
    public void RaiseMartialEffectStartup<T>(EntityUid user, T effect) where T : MartialArtEffect
    {
        var ev = new MartialArtEffectStartupEvent<T>(effect);
        RaiseLocalEvent(user, ev);
    }

    /// <summary>
    /// Should be never called outside of this system
    /// </summary>
    public void RaiseMartialEffectShutdown<T>(EntityUid user, T effect) where T : MartialArtEffect
    {
        var ev = new MartialArtEffectShutdownEvent<T>(effect);
        RaiseLocalEvent(user, ev);
    }
}
