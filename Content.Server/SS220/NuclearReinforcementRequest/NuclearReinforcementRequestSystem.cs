// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Server.NukeOps;
using Content.Server.Store.Systems;
using Content.Shared.Atmos.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Components;
using Content.Shared.NukeOps;
using Content.Shared.Objectives.Systems;
using Content.Shared.Popups;
using Content.Shared.SS220.NuclearReinforcementRequest;
using Content.Shared.Store;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.NuclearReinforcementRequest;

public sealed partial class NuclearReinforcementRequestSystem : EntitySystem
{
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private TargetSystem _target = default!;
    [Dependency] private SharedPopupSystem _popupSystem = default!;
    [Dependency] private StoreSystem _store = default!;

    private static readonly ProtoId<CurrencyPrototype> TelecrystalCurrencyPrototype = "Telecrystal";

    private static readonly LocId InsufficientUses = "reinforcement-uplink-insufficient-uses";

    private static readonly LocId ReinforcementsGranted = "reinforcement-uplink-reinforcements-granted";

    private static readonly LocId NoReinforcementsGranted = "reinforcement-uplink-no-reinforcements-granted";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NuclearReinforcementRequestComponent, UseInHandEvent>(OnUseInHand);
    }

    public void OnUseInHand(Entity<NuclearReinforcementRequestComponent> ent, ref UseInHandEvent args)
    {
        if (ent.Comp.UsesRemain <= 0)
        {
            _popupSystem.PopupEntity(Loc.GetString(InsufficientUses), ent);
            return;
        }

        var allAliveHumanoids = _target.GetAliveHumans();
        var allOps = _entityManager.AllComponents<NukeOperativeComponent>();

        int toSpawn = (allAliveHumanoids.Count - allOps.Length) / 10;

        if (toSpawn <= 0)
        {
            _popupSystem.PopupEntity(Loc.GetString(NoReinforcementsGranted), ent);
            return;
        }

        _popupSystem.PopupEntity(Loc.GetString(ReinforcementsGranted), ent);

        bool warDeclared = false;

        var declaratorQuery = EntityQueryEnumerator<WarDeclaratorComponent>();

        while (declaratorQuery.MoveNext(out var uid, out var declaratorComp))
        {
            if (declaratorComp.CurrentStatus == WarConditionStatus.WarReady)
            {
                warDeclared = true;
                break;
            }
        }

        for (int i = 0; i < toSpawn; ++i)
        {
            var uplinkUid = Spawn(ent.Comp.UplinkProto, Transform(args.User).Coordinates);

            if (warDeclared)
                _store.TryAddCurrency(new() { { TelecrystalCurrencyPrototype, ent.Comp.TelecrystalsToAdd } }, uplinkUid);

            Spawn(ent.Comp.ReinforcementProto, Transform(args.User).Coordinates);
        }

        ent.Comp.UsesRemain -= 1;
    }
}
