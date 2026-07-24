// SS220 Changeling
using Content.Shared.Actions;
using Content.Shared.Administration.Logs;
using Content.Shared.Body;
using Content.Shared.Changeling.Components;
using Content.Shared.Cloning;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared.Changeling.Systems;

public sealed partial class ChangelingTransformSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedCloningSystem _cloning = default!;
    [Dependency] private readonly SharedVisualBodySystem _visualBody = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IdentitySystem _identity = default!;
    [Dependency] private readonly SharedChangelingIdentitySystem _changelingIdentity = default!;

    private const string ChangelingBuiXmlGeneratedName = "ChangelingTransformBoundUserInterface";
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingTransformComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ChangelingTransformComponent, ChangelingTransformActionEvent>(OnTransformAction);
        SubscribeLocalEvent<ChangelingTransformComponent, ChangelingTransformIdentitySelectMessage>(OnTransformSelected);
        SubscribeLocalEvent<ChangelingTransformComponent, ChangelingTransformIdentityDropMessage>(OnTransformDrop);
        SubscribeLocalEvent<ChangelingTransformComponent, ChangelingTransformDoAfterEvent>(OnSuccessfulTransform);
        SubscribeLocalEvent<ChangelingTransformComponent, ComponentShutdown>(OnShutdown);

        // Components that need special handling outside of cloning.
        SubscribeLocalEvent<StorageComponent, BeforeChangelingTransformEvent>(StorageBeforeTransform);
    }

    private void OnStartup(Entity<ChangelingTransformComponent> ent, ref ComponentStartup init)
    {
        if (ent.Comp.TransformWindup < TimeSpan.Zero)
            ent.Comp.TransformWindup = TimeSpan.Zero;
        ent.Comp.ChemicalCost = FixedPoint2.Max(FixedPoint2.Zero, ent.Comp.ChemicalCost);
        _actions.AddAction(ent, ref ent.Comp.ChangelingTransformActionEntity, ent.Comp.ChangelingTransformAction);

        var userInterfaceComp = EnsureComp<UserInterfaceComponent>(ent);
        _ui.SetUi((ent, userInterfaceComp), ChangelingTransformUiKey.Key, new InterfaceData(ChangelingBuiXmlGeneratedName));
    }

    private void OnShutdown(Entity<ChangelingTransformComponent> ent, ref ComponentShutdown args)
    {
        if (_net.IsServer)
            ent.Comp.CurrentTransformSound = _audio.Stop(ent.Comp.CurrentTransformSound);

        _ui.CloseUi(ent.Owner, ChangelingTransformUiKey.Key);

        if (_net.IsServer &&
            !TerminatingOrDeleted(ent.Owner) &&
            ent.Comp.ChangelingTransformActionEntity != null)
        {
            _actions.RemoveAction(ent.Owner, ent.Comp.ChangelingTransformActionEntity);
            QueueDel(ent.Comp.ChangelingTransformActionEntity.Value);
        }
    }

    private void OnTransformAction(Entity<ChangelingTransformComponent> ent,
        ref ChangelingTransformActionEvent args)
    {
        if (args.Handled || args.Performer != ent.Owner)
            return;

        if (!TryComp<UserInterfaceComponent>(ent, out var userInterfaceComp))
            return;

        if (!TryComp<ChangelingIdentityComponent>(ent, out var identity))
            return;

        args.Handled = true;

        var onlyCurrentIdentityRemains = identity.StoredIdentities.Count == 1 &&
                                         identity.CurrentIdentity is { } current &&
                                         identity.StoredIdentities.Contains(current);
        if (identity.StoredIdentities.Count == 0 || onlyCurrentIdentityRemains)
        {
            _popup.PopupClient(Loc.GetString("changeling-transform-no-identities"), ent.Owner, ent.Owner);
            return;
        }

        if (!_ui.IsUiOpen((ent, userInterfaceComp), ChangelingTransformUiKey.Key, args.Performer))
            _ui.OpenUi((ent, userInterfaceComp), ChangelingTransformUiKey.Key, args.Performer);
    }

    private void OnTransformSelected(Entity<ChangelingTransformComponent> ent,
        ref ChangelingTransformIdentitySelectMessage args)
    {
        if (!_net.IsServer || args.Actor != ent.Owner)
            return;

        if (!TryGetEntity(args.TargetIdentity, out var targetIdentity))
            return;

        if (!TryComp<ChangelingIdentityComponent>(ent, out var identity))
            return;

        if (identity.CurrentIdentity == targetIdentity)
            return; // don't transform into ourselves

        if (!identity.ConsumedIdentities.ContainsKey(targetIdentity.Value))
            return; // this identity does not belong to this player

        TryTransformInto(ent.AsNullable(), targetIdentity.Value);
    }

    private void OnTransformDrop(Entity<ChangelingTransformComponent> ent,
        ref ChangelingTransformIdentityDropMessage args)
    {
        if (!_net.IsServer || args.Actor != ent.Owner)
            return;

        if (!TryGetEntity(args.TargetIdentity, out var targetIdentity))
            return;

        if (!TryComp<ChangelingIdentityComponent>(ent, out var identity))
            return;

        if (identity.CurrentIdentity == targetIdentity)
            return; // don't drop our current identity

        if (!identity.ConsumedIdentities.ContainsKey(targetIdentity.Value))
            return; // this identity does not belong to this player

        _popup.PopupClient(Loc.GetString("changeling-transform-bui-drop-identity-entity-popup", ("entity", targetIdentity.Value)), ent.Owner, PopupType.Large);
        _changelingIdentity.TryDropStoredIdentity(ent.Owner, targetIdentity.Value);
    }

    /// <summary>
    /// Transform the changeling into another identity.
    /// The target must be one of this changeling's stored identity snapshots.
    /// </summary>
    public bool TryTransformInto(Entity<ChangelingTransformComponent?> ent, EntityUid targetIdentity)
    {
        if (!Resolve(ent, ref ent.Comp, false) ||
            TerminatingOrDeleted(targetIdentity) ||
            !TryComp<ChangelingIdentityComponent>(ent, out var identity) ||
            identity.CurrentIdentity == targetIdentity ||
            !identity.ConsumedIdentities.ContainsKey(targetIdentity))
        {
            return false;
        }

        if (_net.IsServer)
            ent.Comp.CurrentTransformSound = _audio.Stop(ent.Comp.CurrentTransformSound);

        if (TryComp<ChangelingStoredIdentityComponent>(targetIdentity, out var storedIdentity) && storedIdentity.OriginalSession != null)
            _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(ent.Owner):player} begun an attempt to transform into \"{Name(targetIdentity)}\" ({storedIdentity.OriginalSession:player}) ");
        else
            _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(ent.Owner):player} begun an attempt to transform into \"{Name(targetIdentity)}\"");

        return _doAfter.TryStartDoAfter(new DoAfterArgs(
            EntityManager,
            ent,
            ent.Comp.TransformWindup,
            new ChangelingTransformDoAfterEvent(),
            ent,
            target: targetIdentity)
        {
            BreakOnMove = true,
            BreakOnWeightlessMove = true,
            BlockDuplicate = true,
            CancelDuplicate = false,
            DuplicateCondition = DuplicateConditions.SameEvent,
            RequireCanInteract = false,
            DistanceThreshold = null,
        });
    }

    private void OnSuccessfulTransform(Entity<ChangelingTransformComponent> ent,
        ref ChangelingTransformDoAfterEvent args)
    {
        args.Handled = true;
        ent.Comp.CurrentTransformSound = _audio.Stop(ent.Comp.CurrentTransformSound);

        if (!_net.IsServer || args.Cancelled)
            return;

        if (!_prototype.Resolve(ent.Comp.TransformCloningSettings, out var settings))
            return;

        if (args.Target is not { } targetIdentity)
            return;

        if (!Exists(targetIdentity) ||
            !TryComp<ChangelingIdentityComponent>(ent, out var identity) ||
            !identity.ConsumedIdentities.ContainsKey(targetIdentity))
            return;

        var chemicalCost = FixedPoint2.Max(FixedPoint2.Zero, ent.Comp.ChemicalCost);
        var spend = new ChangelingChemicalSpendAttemptEvent(chemicalCost);
        RaiseLocalEvent(ent.Owner, ref spend);
        if (spend.Cancelled)
        {
            _popup.PopupEntity(Loc.GetString("changeling-not-enough-chemicals"), ent.Owner, ent.Owner);
            return;
        }

        var beforeTransformEvent = new BeforeChangelingTransformEvent(targetIdentity);
        RaiseLocalEvent(args.User, beforeTransformEvent);

        var visibleUser = Identity.Entity(ent.Owner, EntityManager);
        var selfMessage = Loc.GetString("changeling-transform-attempt-self", ("user", visibleUser));
        var othersMessage = Loc.GetString("changeling-transform-attempt-others", ("user", visibleUser));

        _visualBody.CopyAppearanceFrom(targetIdentity, args.User);
        _cloning.CloneComponents(targetIdentity, args.User, settings);

        ent.Comp.CurrentTransformSound = _audio.PlayPvs(ent.Comp.TransformAttemptNoise, ent)?.Entity;
        _popup.PopupEntity(selfMessage, ent.Owner, ent.Owner, PopupType.MediumCaution);
        _popup.PopupEntity(othersMessage, ent.Owner, Filter.PvsExcept(ent.Owner), true, PopupType.MediumCaution);

        if (TryComp<ChangelingStoredIdentityComponent>(targetIdentity, out var storedIdentity) && storedIdentity.OriginalSession != null)
            _adminLogger.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(ent.Owner):player} successfully transformed into \"{Name(targetIdentity)}\" ({storedIdentity.OriginalSession:player})");
        else
            _adminLogger.Add(LogType.Action, LogImpact.High, $"{ToPrettyString(ent.Owner):player} successfully transformed into \"{Name(targetIdentity)}\"");

        _metaData.SetEntityName(ent, Name(targetIdentity), raiseEvents: false); // Don't raise events because we don't want to rename the ID card.
        _identity.QueueIdentityUpdate(ent); // We have to manually refresh the identity because we did not raise events.

        Dirty(ent);

        identity.CurrentGenome = identity.StoredGenomes.GetValueOrDefault(targetIdentity);
        identity.CurrentIdentity = null;
        Dirty(ent.Owner, identity);
        _changelingIdentity.TryDropStoredIdentity(ent.Owner, targetIdentity);

        var afterTransformEvent = new AfterChangelingTransformEvent(targetIdentity);
        RaiseLocalEvent(args.User, afterTransformEvent);
    }

    private void StorageBeforeTransform(Entity<StorageComponent> ent, ref BeforeChangelingTransformEvent args)
    {
        if (HasComp<StorageComponent>(args.StoredIdentity))
            return; // If we have a storage component and the target has one as well, then do nothing.

        // If the target identity does not have a storage anymore, drop all items inside our storage so that they don't become unreachable.
        _container.EmptyContainer(ent.Comp.Container);
    }
}
