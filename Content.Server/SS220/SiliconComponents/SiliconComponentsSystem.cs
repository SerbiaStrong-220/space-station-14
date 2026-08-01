// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Server.Traitor.Uplink;
using Content.Shared.SS220.SiliconComponents;
using Content.Shared.Store.Components;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.SiliconComponents;

public sealed partial class SiliconComponentsSystem : SharedSiliconComponentsSystem
{
    [Dependency] private UplinkSystem _uplink = default!;
    [Dependency] private SharedContainerSystem _container = default!;

    public static readonly EntProtoId<StoreComponent> HiddenUplink = "SynthModHiddenUplink";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SiliconComponentsComponent, FallbackUplinkRequiredEvent>(OnFallbackUplinkRequest);
    }

    private void OnFallbackUplinkRequest(Entity<SiliconComponentsComponent> ent, ref FallbackUplinkRequiredEvent args)
    {
        if (ent.Comp.ModuleContainer == null)
            return;

        var uplinkEnt = Spawn(HiddenUplink, MapCoordinates.Nullspace);

        if (!_container.Insert(uplinkEnt, ent.Comp.ModuleContainer))
            return;

        if (_uplink.TryAddEntityUplink(ent, args.Balance, out var generatedCode, uplinkEnt, uplinkEnt, args.GiveDiscounts, false, args.UseDynamics, mustHaveCode: false))
            args.Handled = true;

        //_ringer.SetBoundUplinkEntity((storeEntity, accessComp), uplinkEntity.Value);
        //_uplink.SetUplink(ent, storeEntity, args.Balance, args.GiveDiscounts, args.UseDynamics);
        //args.Handled = true;
    }
}
