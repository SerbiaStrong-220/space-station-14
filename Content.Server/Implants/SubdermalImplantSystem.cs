using Content.Server.Actions;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Server.Cuffs;
using Content.Server.Forensics;
using Content.Server.Humanoid;
using Content.Server.Implants.Components;
using Content.Server.Store.Components;
using Content.Server.Store.Systems;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Cuffs.Components;
using Content.Shared.Forensics;
using Content.Shared.Forensics.Components;
using Content.Shared.Humanoid;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Preferences;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using System.Numerics;
using Content.Server.IdentityManagement;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Server.Polymorph.Systems;
using Content.Server.SS220.PenScrambler;
using Content.Shared.Actions;
using Content.Shared.DetailExaminable;
using Content.Shared.Store.Components;
using Robust.Shared.Collections;
using Robust.Shared.Map.Components;
using Content.Shared.DoAfter;
using Content.Shared.Polymorph;
using Content.Shared.SS220.PenScrambler;
using Content.Shared.FixedPoint;
using Content.Shared.SS220.Store;
using Content.Shared.Charges.Components;

namespace Content.Server.Implants;

public sealed class SubdermalImplantSystem : SharedSubdermalImplantSystem
{
    [Dependency] private readonly CuffableSystem _cuffable = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoidAppearance = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly ForensicsSystem _forensicsSystem = default!;
    [Dependency] private readonly PullingSystem _pullingSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly IdentitySystem _identity = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!; //SS220-insert-currency-doafter
    [Dependency] private readonly PolymorphSystem _polymorph = default!; //ss220 add dna copy implant
    [Dependency] private readonly SharedActionsSystem _actions = default!; //ss220 add adrenal implant

    private EntityQuery<PhysicsComponent> _physicsQuery;
    private HashSet<Entity<MapGridComponent>> _targetGrids = [];

    private const string BeakerSolution = "beaker"; //ss220 add adrenal implant
    private const string ChemicalSolution = "chemicals"; //ss220 add adrenal implant

    public override void Initialize()
    {
        base.Initialize();

        _physicsQuery = GetEntityQuery<PhysicsComponent>();

        SubscribeLocalEvent<SubdermalImplantComponent, UseChemicalImplantEvent>(OnChemicaImplant); // SS220 - chemical-implants start
        SubscribeLocalEvent<SubdermalImplantComponent, UseFreedomImplantEvent>(OnFreedomImplant);
        SubscribeLocalEvent<StoreComponent, ImplantRelayEvent<AfterInteractUsingEvent>>(OnStoreRelay);
        SubscribeLocalEvent<SubdermalImplantComponent, ActivateImplantEvent>(OnActivateImplantEvent);
        SubscribeLocalEvent<SubdermalImplantComponent, UseScramImplantEvent>(OnScramImplant);
        SubscribeLocalEvent<SubdermalImplantComponent, UseDnaScramblerImplantEvent>(OnDnaScramblerImplant);
        SubscribeLocalEvent<SubdermalImplantComponent, UseAdrenalImplantEvent>(OnAdrenalImplant); //ss220 add adrenal implant

        SubscribeLocalEvent<SubdermalImplantComponent, UseDnaCopyImplantEvent>(OnDnaCopyImplant); //ss220 dna copy implant add
    }

    // SS220 - chemical-implants start
    private void OnChemicaImplant(EntityUid uid, SubdermalImplantComponent component, UseChemicalImplantEvent args)
    {
        if (component.ImplantedEntity is not { } ent)
            return;
        if (!TryComp<SolutionContainerManagerComponent>(args.Performer, out var _performerSolutionComp)
            || !TryComp<SolutionContainerManagerComponent>(uid, out var _implantSolutionComp))
            return;

        if (!_solutionContainer.TryGetSolution(new(args.Performer, _performerSolutionComp), "chemicals", out var chemicalSolution))
            return;

        if (!_solutionContainer.TryGetSolution(new(uid, _implantSolutionComp), "beaker", out var beakerSolution))
            return;

        _solutionContainer.TryTransferSolution(chemicalSolution.Value, beakerSolution.Value.Comp.Solution, beakerSolution.Value.Comp.Solution.Volume);

        args.Handled = true;

        QueueDel(uid);

    }
    // SS220 - chemical-implants end

    private void OnStoreRelay(EntityUid uid, StoreComponent store, ImplantRelayEvent<AfterInteractUsingEvent> implantRelay)
    {
        var args = implantRelay.Event;

        if (args.Handled)
            return;

        // can only insert into yourself to prevent uplink checking with renault
        if (args.Target != args.User)
            return;

        if (!TryComp<CurrencyComponent>(args.Used, out var currency))
            return;

        //SS220-insert-currency-doafter begin
        if (store.CurrencyInsertTime != null)
        {
            var doAfter = new DoAfterArgs(EntityManager, args.User, store.CurrencyInsertTime.Value,
                new InsertCurrencyDoAfterEvent(args.Used, (uid, store)),
                uid)
            {
                NeedHand = true,
                BreakOnDamage = true
            };

            _doAfter.TryStartDoAfter(doAfter);
            args.Handled = true;
            return;
        }
        //SS220-insert-currency-doafter end

        // same as store code, but message is only shown to yourself
        if (!_store.TryAddCurrency((args.Used, currency), (uid, store)))
            return;

        args.Handled = true;
        var msg = Loc.GetString("store-currency-inserted-implant", ("used", args.Used));
        _popup.PopupEntity(msg, args.User, args.User);
    }

    private void OnFreedomImplant(EntityUid uid, SubdermalImplantComponent component, UseFreedomImplantEvent args)
    {
        if (!TryComp<CuffableComponent>(component.ImplantedEntity, out var cuffs) || cuffs.Container.ContainedEntities.Count < 1)
            return;

        _cuffable.Uncuff(component.ImplantedEntity.Value, cuffs.LastAddedCuffs, cuffs.LastAddedCuffs);
        args.Handled = true;
    }

    private void OnActivateImplantEvent(EntityUid uid, SubdermalImplantComponent component, ActivateImplantEvent args)
    {
        args.Handled = true;
    }

    private void OnScramImplant(EntityUid uid, SubdermalImplantComponent component, UseScramImplantEvent args)
    {
        if (component.ImplantedEntity is not { } ent)
            return;

        if (!TryComp<ScramImplantComponent>(uid, out var implant))
            return;

        // We need stop the user from being pulled so they don't just get "attached" with whoever is pulling them.
        // This can for example happen when the user is cuffed and being pulled.
        if (TryComp<PullableComponent>(ent, out var pull) && _pullingSystem.IsPulled(ent, pull))
            _pullingSystem.TryStopPull(ent, pull);

        // Check if the user is pulling anything, and drop it if so
        if (TryComp<PullerComponent>(ent, out var puller) && TryComp<PullableComponent>(puller.Pulling, out var pullable))
            _pullingSystem.TryStopPull(puller.Pulling.Value, pullable);

        var xform = Transform(ent);
        var targetCoords = SelectRandomTileInRange(xform, implant.TeleportRadius);

        if (targetCoords != null)
        {
            _xform.SetCoordinates(ent, targetCoords.Value);
            _audio.PlayPvs(implant.TeleportSound, ent);
            args.Handled = true;
        }
    }

    private EntityCoordinates? SelectRandomTileInRange(TransformComponent userXform, float radius)
    {
        var userCoords = _xform.ToMapCoordinates(userXform.Coordinates);
        _targetGrids.Clear();
        _lookupSystem.GetEntitiesInRange(userCoords, radius, _targetGrids);
        Entity<MapGridComponent>? targetGrid = null;

        if (_targetGrids.Count == 0)
            return null;

        // Give preference to the grid the entity is currently on.
        // This does not guarantee that if the probability fails that the owner's grid won't be picked.
        // In reality the probability is higher and depends on the number of grids.
        if (userXform.GridUid != null && TryComp<MapGridComponent>(userXform.GridUid, out var gridComp))
        {
            var userGrid = new Entity<MapGridComponent>(userXform.GridUid.Value, gridComp);
            if (_random.Prob(0.5f))
            {
                _targetGrids.Remove(userGrid);
                targetGrid = userGrid;
            }
        }

        if (targetGrid == null)
            targetGrid = _random.GetRandom().PickAndTake(_targetGrids);

        EntityCoordinates? targetCoords = null;

        do
        {
            var valid = false;

            var range = (float) Math.Sqrt(radius);
            var box = Box2.CenteredAround(userCoords.Position, new Vector2(range, range));
            var tilesInRange = _mapSystem.GetTilesEnumerator(targetGrid.Value.Owner, targetGrid.Value.Comp, box, false);
            var tileList = new ValueList<Vector2i>();

            while (tilesInRange.MoveNext(out var tile))
            {
                tileList.Add(tile.GridIndices);
            }

            while (tileList.Count != 0)
            {
                var tile = tileList.RemoveSwap(_random.Next(tileList.Count));
                valid = true;
                foreach (var entity in _mapSystem.GetAnchoredEntities(targetGrid.Value.Owner, targetGrid.Value.Comp,
                             tile))
                {
                    if (!_physicsQuery.TryGetComponent(entity, out var body))
                        continue;

                    if (body.BodyType != BodyType.Static ||
                        !body.Hard ||
                        (body.CollisionLayer & (int) CollisionGroup.MobMask) == 0)
                        continue;

                    valid = false;
                    break;
                }

                if (valid)
                {
                    targetCoords = new EntityCoordinates(targetGrid.Value.Owner,
                        _mapSystem.TileCenterToVector(targetGrid.Value, tile));
                    break;
                }
            }

            if (valid || _targetGrids.Count == 0) // if we don't do the check here then PickAndTake will blow up on an empty set.
                break;

            targetGrid = _random.GetRandom().PickAndTake(_targetGrids);
        } while (true);

        return targetCoords;
    }

    private void OnDnaScramblerImplant(EntityUid uid, SubdermalImplantComponent component, UseDnaScramblerImplantEvent args)
    {
        if (component.ImplantedEntity is not { } ent)
            return;

        if (TryComp<HumanoidAppearanceComponent>(ent, out var humanoid))
        {
            var newProfile = HumanoidCharacterProfile.RandomWithSpecies(humanoid.Species);
            _humanoidAppearance.LoadProfile(ent, newProfile, humanoid);
            _metaData.SetEntityName(ent, newProfile.Name, raiseEvents: false); // raising events would update ID card, station record, etc.

            // If the entity has the respecive components, then scramble the dna and fingerprint strings
            _forensicsSystem.RandomizeDNA(ent);
            _forensicsSystem.RandomizeFingerprint(ent);

            RemComp<DetailExaminableComponent>(ent); // remove MRP+ custom description if one exists
            _identity.QueueIdentityUpdate(ent); // manually queue identity update since we don't raise the event
            _popup.PopupEntity(Loc.GetString("scramble-implant-activated-popup", ("identity", newProfile.Name)), ent, ent); //ss220 fix locale
        }

        args.Handled = true;
        QueueDel(uid);
    }

    //ss220 dna copy implant add start
    private void OnDnaCopyImplant(Entity<SubdermalImplantComponent> ent, ref UseDnaCopyImplantEvent args)
    {
        if (!TryComp<TransferIdentityComponent>(ent.Owner, out var transferIdentityComponent))
            return;

        var clone = transferIdentityComponent.NullspaceClone;

        if (clone == null)
        {
            QueueDel(ent);
            return;
        }

        if (ent.Comp.ImplantedEntity is not { } user)
            return;

        if (TryComp<HumanoidAppearanceComponent>(user, out var userAppearanceComp))
        {

            if (!TryComp<HumanoidAppearanceComponent>(clone, out var cloneAppearanceComp))
                return;

            _humanoidAppearance.CloneAppearance(clone.Value, user, cloneAppearanceComp, userAppearanceComp);

            _metaData.SetEntityName(user, MetaData(clone.Value).EntityName, raiseEvents: false);

            if (TryComp<DnaComponent>(user, out var dna)
                && TryComp<DnaComponent>(clone.Value, out var dnaClone) &&
                dnaClone.DNA != null)
            {
                dna.DNA = dnaClone.DNA;
                var ev = new GenerateDnaEvent { Owner = user, DNA = dna.DNA };
                RaiseLocalEvent(ent, ref ev);
            }

            if (TryComp<FingerprintComponent>(user, out var fingerprint)
                && TryComp<FingerprintComponent>(clone.Value, out var fingerprintTarget))
            {
                fingerprint.Fingerprint = fingerprintTarget.Fingerprint;
            }

            var setScale = EnsureComp<SetScaleFromTargetComponent>(user);
            setScale.Target = GetNetEntity(clone);

            Dirty(user, setScale);

            var evEvent = new SetScaleFromTargetEvent(GetNetEntity(user), setScale.Target);
            RaiseNetworkEvent(evEvent);

            _identity.QueueIdentityUpdate(user);

            _popup.PopupEntity(Loc.GetString("pen-scrambler-success-convert-to-identity", ("identity", MetaData(clone.Value).EntityName)), user, user);
        }

        QueueDel(clone);
        QueueDel(ent);
    }
    //ss220 dna copy implant add end

    //ss220 add adrenal implant start
    private void OnAdrenalImplant(Entity<SubdermalImplantComponent> ent, ref UseAdrenalImplantEvent args)
    {
        if (!TryComp<SolutionContainerManagerComponent>(ent.Owner, out var solutionImplantComp))
            return;

        if (!TryComp<SolutionContainerManagerComponent>(args.Performer, out var solutionUserComp))
            return;

        if (!_solutionContainer.TryGetSolution((ent.Owner, solutionImplantComp), BeakerSolution, out var solutionImplant))
            return;

        if (!_solutionContainer.TryGetSolution((args.Performer, solutionUserComp), ChemicalSolution, out var solutionUser))
            return;

        var quantity = solutionImplant.Value.Comp.Solution.Volume;
        if (TryComp<LimitedChargesComponent>(args.Action, out var actionCharges))
            quantity /= actionCharges.MaxCharges;

        _solutionContainer.TryTransferSolution(solutionUser.Value,
            solutionImplant.Value.Comp.Solution,
            quantity);

        args.Handled = true;
    }
    //ss220 add adrenal implant end
}
