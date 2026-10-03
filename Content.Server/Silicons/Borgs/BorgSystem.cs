using Content.Server.Administration.Managers;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Hands.Systems;
using Content.Server.SS220.Events;
using Content.Shared.Alert;
using Content.Shared.Body.Events;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage.Systems;
using Content.Shared.Emag.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.Roles;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Trigger.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.Silicons.Borgs;

/// <inheritdoc/>
public sealed partial class BorgSystem : SharedBorgSystem
{
    [Dependency] private IBanManager _banManager = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private TriggerSystem _trigger = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!; // SS220 Borgs-Id-fix
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private EmagSystem _emag = default!;
    [Dependency] private MobThresholdSystem _mobThresholdSystem = default!;
    [Dependency] private ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;
    [Dependency] private DamageableSystem _damageable = default!;

    public static readonly ProtoId<JobPrototype> BorgJobId = "Borg";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BorgChassisComponent, GetInsteadIdCardNameEvent>(OnGetBorgName); // SS220 Borgs-Id-fix

        InitializeTransponder();
    }

    public override bool CanPlayerBeBorged(ICommonSession session)
    {
        if (_banManager.GetJobBans(session.UserId)?.Contains(BorgJobId) == true)
            return false;

        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateTransponder(frameTime);
    }

    // SS220 Borgs-Id-fix start
    private void OnGetBorgName(EntityUid uid, BorgChassisComponent component, ref GetInsteadIdCardNameEvent args)
    {
        if (TryComp<BorgSwitchableTypeComponent>(uid, out var switchComp)
            && switchComp.SelectedBorgType is not null
            && _protoManager.TryIndex<BorgTypePrototype>(switchComp.SelectedBorgType, out var borgType))
            args.Name = Loc.GetString(borgType.Name);
        else
            args.Name = Loc.GetString("borg-type-prototype-generic");
    }
    // SS220 Borgs-Id-fix end
}
