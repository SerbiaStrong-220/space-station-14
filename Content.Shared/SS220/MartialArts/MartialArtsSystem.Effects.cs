// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Interaction.Events;
using Content.Shared.SS220.MartialArts.Effects;

namespace Content.Shared.SS220.MartialArts;

public sealed partial class MartialArtsSystem
{
    // private void InitializeEffectsRelay()
    // {
    //     SubscribeLocalEvent<MartialArtistComponent, AttackAttemptEvent>(HandleRelay);
    // }

    // private void HandleRelay<T>(EntityUid user, MartialArtistComponent artist, ref T ev)
    // {
    //     Log.Info($"Received relay {ev} event");

    //     var effects = GetMartialArtEffects(user, artist);

    //     foreach (var effect in effects)
    //     {
    //         RaiseRelayEvent(user, ref ev, effect);
    //     }
    // }

    // private void RaiseRelayEvent<T, TEffect>(EntityUid user, ref T ev, TEffect effect) where TEffect : BaseMartialArtEffect
    // {
    //     Log.Info($"Raise relayed event {ev} for effect {effect}");

    //     var relayEvent = new MartialArtEffectRelayEvent<T, TEffect>(ev, effect);
    //     RaiseLocalEvent(user, relayEvent);

    //     ev = relayEvent.Event;
    // }

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
