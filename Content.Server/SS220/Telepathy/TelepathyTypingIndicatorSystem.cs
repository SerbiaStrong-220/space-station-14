// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chat.TypingIndicator;
using Content.Shared.SS220.Telepathy;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.SS220.Telepathy;

/// <summary>
/// Replicates telepathic typing only to recipients of the sender's channel, within ordinary entity visibility.
/// </summary>
public sealed class TelepathyTypingIndicatorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private GameTick _lastPermissionsCheck;
    private bool _permissionsDirty;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TypingIndicatorComponent, ComponentStartup>(OnIndicatorStartup);
        SubscribeLocalEvent<TelepathyTypingIndicatorComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<TelepathyTypingIndicatorComponent, PlayerDetachedEvent>(OnDetached);
        SubscribeLocalEvent<PlayerAttachedEvent>(OnAttached);
        SubscribeLocalEvent<TelepathyChangedEvent>(OnTelepathyChanged);
        SubscribeLocalEvent<TelepathyTypingChangedEvent>(OnTypingChanged);
    }

    private void OnIndicatorStartup(Entity<TypingIndicatorComponent> ent, ref ComponentStartup args)
    {
        // Never expose telepathy through the presence of the private indicator component.
        EnsureComp<TelepathyTypingIndicatorComponent>(ent);
    }

    private void OnAttached(PlayerAttachedEvent args)
    {
        _permissionsDirty = true;
    }

    private void OnDetached(Entity<TelepathyTypingIndicatorComponent> ent, ref PlayerDetachedEvent args)
    {
        SetState(ent, TypingIndicatorState.None);
        _permissionsDirty = true;
    }

    private void OnTelepathyChanged(ref TelepathyChangedEvent args)
    {
        _permissionsDirty = true;
    }

    private void OnTypingChanged(ref TelepathyTypingChangedEvent args)
    {
        if (!TryComp<TelepathyTypingIndicatorComponent>(args.Sender, out var indicator))
            return;

        if (!args.IsTelepathy)
        {
            SetState((args.Sender, indicator), TypingIndicatorState.None);
            return;
        }

        if (!CanSend(args.Sender))
        {
            SetState((args.Sender, indicator), TypingIndicatorState.None);
            return;
        }

        var state = args.State;
        if (state == TypingIndicatorState.Idle)
        {
            SetState((args.Sender, indicator), state);
            return;
        }

        if (state != TypingIndicatorState.Typing)
            state = TypingIndicatorState.None;

        SetState((args.Sender, indicator), state);
    }

    private bool CanSend(EntityUid uid)
    {
        if (!TryComp<TelepathyComponent>(uid, out var telepathy))
            return false;

        if (!telepathy.CanSend)
            return false;

        if (telepathy.TelepathyChannelPrototype == null)
            return false;

        var attempt = new TelepathySendAttemptEvent(uid, false);
        RaiseLocalEvent(uid, ref attempt);
        return !attempt.Cancelled;
    }

    private void OnGetState(Entity<TelepathyTypingIndicatorComponent> ent, ref ComponentGetState args)
    {
        // Explicitly reset former recipients; withholding updates would leave a stale visible indicator.
        var state = CanReceive(ent, args.Player?.AttachedEntity) ? ent.Comp.State : TypingIndicatorState.None;
        args.State = new TelepathyTypingIndicatorState(state);
    }

    private bool CanReceive(EntityUid sender, EntityUid? receiver)
    {
        if (receiver == null)
            return false;

        if (!TryComp<TelepathyComponent>(receiver, out var receiverTelepathy))
            return false;

        if (!TryComp<TelepathyComponent>(sender, out var senderTelepathy))
            return false;

        if (!senderTelepathy.CanSend)
            return false;

        if (senderTelepathy.TelepathyChannelPrototype == null)
            return false;

        if (receiverTelepathy.ReceiveAllChannels)
            return true;

        return receiverTelepathy.TelepathyChannelPrototype == senderTelepathy.TelepathyChannelPrototype;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Telepathy fields may be edited and dirtied directly by conversion/mindslave systems.
        var telepathyQuery = EntityQueryEnumerator<TelepathyComponent>();
        while (telepathyQuery.MoveNext(out var telepathy))
        {
            if (telepathy.LastModifiedTick >= _lastPermissionsCheck)
                _permissionsDirty = true;
        }

        _lastPermissionsCheck = _timing.CurTick;
        var query = EntityQueryEnumerator<TelepathyTypingIndicatorComponent>();
        while (query.MoveNext(out var uid, out var indicator))
        {
            if (indicator.State == TypingIndicatorState.None)
                continue;

            if (!CanSend(uid))
            {
                SetState((uid, indicator), TypingIndicatorState.None);
                continue;
            }

            if (_permissionsDirty)
                Dirty(uid, indicator);
        }

        _permissionsDirty = false;
    }

    private void SetState(Entity<TelepathyTypingIndicatorComponent> ent, TypingIndicatorState state)
    {
        if (ent.Comp.State == state)
            return;

        ent.Comp.State = state;
        Dirty(ent);
    }
}
