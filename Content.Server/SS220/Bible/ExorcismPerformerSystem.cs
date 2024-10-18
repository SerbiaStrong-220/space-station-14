// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Actions;
using Content.Server.Bible.Components;
using Content.Server.Chat.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.SS220.Bible;
using Content.Shared.SS220.CultYogg.Components;
using Content.Shared.SS220.CultYogg.EntitySystems;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;
using Robust.Shared.Spawners;
using Robust.Shared.Player;
using Robust.Shared.Containers;
using Content.Shared.Hands.Components;

namespace Content.Server.SS220.Bible;

public sealed class ExorcismPerformerSystem : SharedExorcismPerformerSystem
{
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly UserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookupSystem = default!;
    [Dependency] private readonly ITimerManager _timerManager = default!;
    [Dependency] private readonly ActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedCultYoggCorruptedSystem _cultYoggCorruptedSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly AppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ExorcismPerformerComponent, GetItemActionsEvent>(GetExorcismAction);
        SubscribeLocalEvent<ExorcismPerformerComponent, ExorcismActionEvent>(OnExorcismAction);
        SubscribeLocalEvent<ExorcismPerformerComponent, ExorcismReadMessage>(OnExorcismRead);
        SubscribeLocalEvent<CultYoggCorruptedComponent, ExorcismPerformedEvent>(OnExorcismPerformedOnCorrupted);
    }

    private void GetExorcismAction(Entity<ExorcismPerformerComponent> entity, ref GetItemActionsEvent args)
    {
        args.AddAction(ref entity.Comp.ExorcismActionEntity, entity.Comp.ExorcismAction);
    }

    private void OnExorcismAction(Entity<ExorcismPerformerComponent> entity, ref ExorcismActionEvent args)
    {
        var user = args.Performer;
        if (!CanUse(entity, user))
        {
            return;
        }

        _uiSystem.SetUiState(entity.Owner, ExorcismUiKey.Key, new ExorcismInterfaceState(entity.Comp.MessageLengthMin, entity.Comp.MessageLengthMax));
        _uiSystem.OpenUi(entity.Owner, ExorcismUiKey.Key, user);
    }

    private void OnExorcismRead(Entity<ExorcismPerformerComponent> entity, ref ExorcismReadMessage message)
    {
        var user = message.Actor;
        if (!CanUse(entity, user))
            return;

        var sanitazedMessage = ExorcismUtils.SanitazeMessage(message.Message);
        var length = sanitazedMessage.Length;
        if (length < entity.Comp.MessageLengthMin || length > entity.Comp.MessageLengthMax)
            return;

        _chat.TrySendInGameICMessage(user, sanitazedMessage, InGameICChatType.Speak, ChatTransmitRange.Normal);

        var entitiesInRange = _entityLookupSystem.GetEntitiesInRange<CultYoggCorruptedComponent>(Transform(user).Coordinates, entity.Comp.Range);
        var args = new ExorcismPerformedEvent(entity, entity.Comp, user);
        RaiseLocalEvent(ref args);
        foreach (var other in entitiesInRange)
        {
            if (_container.TryGetOuterContainer(other, Transform(other), out var container))
                continue;

            RaiseLocalEvent(other, ref args);
        }
        PlayPerformanceEffects((entity, entity.Comp));

        var exorcismAction = entity.Comp.ExorcismActionEntity;
        if (exorcismAction != null && TryComp(exorcismAction, out InstantActionComponent? actionComponent))
            _actionsSystem.SetCooldown(exorcismAction, actionComponent.UseDelay ?? TimeSpan.FromSeconds(1));
    }

    private void OnExorcismPerformedOnCorrupted(Entity<CultYoggCorruptedComponent> entity, ref ExorcismPerformedEvent args)
    {
        var previousEntityString = ToPrettyString(entity);
        var uncorruptedEntity = _cultYoggCorruptedSystem.RevertCorruption(entity, out var recipe);
        var effectPrototype = recipe?.CorruptionReverseEffect;
        _adminLogger.Add(LogType.EntitySpawn, LogImpact.Low, $"{ToPrettyString(args.Performer)} used exorcism on {previousEntityString} and made {ToPrettyString(uncorruptedEntity)}");

        if (uncorruptedEntity == null) return;

        if (effectPrototype != null)
        {
            var coordinates = Transform(uncorruptedEntity.Value).Coordinates;
            var effect = Spawn(effectPrototype, coordinates);
            if (!HasComp<TimedDespawnComponent>(effect))
            {
                var timedDespawn = AddComp<TimedDespawnComponent>(effect);
                timedDespawn.Lifetime = 2f;
            }
        }
    }

    private void PlayPerformanceEffects(Entity<ExorcismPerformerComponent> entity)
    {
        _appearanceSystem.SetData(entity, ExorcismPerformerVisualState.State, ExorcismPerformerVisualState.Performing);
        _timerManager.AddTimer(new Timer((int) (entity.Comp.LightEffectDurationSeconds * 1000), false, () =>
        {
            _appearanceSystem.SetData(entity, ExorcismPerformerVisualState.State, ExorcismPerformerVisualState.None);
        }));
    }

    private bool CanUse(Entity<ExorcismPerformerComponent> entity, EntityUid user)
    {
        if (!TryComp(user, out ActorComponent? actor))
            return false;
        if (entity.Comp.RequiresBibleUser && !HasComp<BibleUserComponent>(user))
        {
            _popupSystem.PopupEntity(Loc.GetString("prayer-popup-notify-pray-locked"), user, actor.PlayerSession, PopupType.Large);
            return false;
        }
        if (entity.Comp.Deleted || Deleted(entity))
            return false;
        if (!_blocker.CanInteract(user, entity))
            return false;
        return true;
    }
}
