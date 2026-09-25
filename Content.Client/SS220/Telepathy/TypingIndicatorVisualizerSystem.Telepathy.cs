// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chat.TypingIndicator;
using Content.Shared.SS220.Telepathy;
using Robust.Shared.GameStates;

namespace Content.Client.Chat.TypingIndicator;

public sealed partial class TypingIndicatorVisualizerSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TelepathyTypingIndicatorComponent, ComponentGetState>(OnTelepathyGetState);
        SubscribeLocalEvent<TelepathyTypingIndicatorComponent, ComponentHandleState>(OnTelepathyHandleState);
        SubscribeLocalEvent<TelepathyTypingIndicatorComponent, ComponentShutdown>(OnTelepathyShutdown);
    }

    private void OnTelepathyGetState(Entity<TelepathyTypingIndicatorComponent> ent, ref ComponentGetState args)
    {
        args.State = new TelepathyTypingIndicatorState(ent.Comp.State);
    }

    private void OnTelepathyHandleState(Entity<TelepathyTypingIndicatorComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not TelepathyTypingIndicatorState state)
            return;

        ent.Comp.State = state.State;
        QueueTelepathyAppearance(ent);
    }

    private void OnTelepathyShutdown(Entity<TelepathyTypingIndicatorComponent> ent, ref ComponentShutdown args)
    {
        ent.Comp.State = TypingIndicatorState.None;
        QueueTelepathyAppearance(ent);
    }

    private void QueueTelepathyAppearance(EntityUid uid)
    {
        if (TryComp<AppearanceComponent>(uid, out var appearance))
            AppearanceSystem.QueueUpdate(uid, appearance);
    }

    private TypingIndicatorState GetTelepathyTypingState(EntityUid uid, TypingIndicatorState publicState)
    {
        if (publicState != TypingIndicatorState.None)
            return publicState;

        if (!TryComp<TelepathyTypingIndicatorComponent>(uid, out var telepathy))
            return publicState;

        return telepathy.State;
    }
}
