// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Interaction.Events;
using Content.Shared.SS220.MartialArts.Effects;

namespace Content.Shared.SS220.MartialArts;

public sealed partial class MartialArtsSystem
{
    private void StartupEffects(EntityUid user, MartialArtPrototype martialArt)
    {
        foreach (var effect in martialArt.Effects)
        {
            RaiseMartialEffectStartup(user, effect);
        }
    }

    private void ShutdownEffects(EntityUid user, MartialArtPrototype martialArt)
    {
        foreach (var effect in martialArt.Effects)
        {
            RaiseMartialEffectShutdown(user, effect);
        }
    }

    private void RaiseMartialEffectStartup<T>(EntityUid user, T effect) where T : BaseMartialArtEffect
    {
        var ev = new MartialArtEffectStartupEvent<T>(effect);
        RaiseLocalEvent(user, ev);
    }

    private void RaiseMartialEffectShutdown<T>(EntityUid user, T effect) where T : BaseMartialArtEffect
    {
        var ev = new MartialArtEffectShutdownEvent<T>(effect);
        RaiseLocalEvent(user, ev);
    }
}
