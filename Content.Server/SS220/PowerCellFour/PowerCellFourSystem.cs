using Content.Server.Power.Components;
using Content.Shared.SS220.PowerCellFour;

namespace Content.Server.SS220.PowerCellFour;

public sealed class PowerCellFourSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PowerCellFourComponent, ChargeChangedEvent>(OnChargeChanged);
    }

    private void OnChargeChanged(Entity<PowerCellFourComponent> ent, ref ChargeChangedEvent args)
    {
        switch (args.Charge*100f/args.MaxCharge)
        {
            case 0f:
                _appearance.SetData(ent.Owner, PowerCellFourVisual.State, PowerCellFourVisualStates.Base);
                break;
            case <= 25f:
                _appearance.SetData(ent.Owner, PowerCellFourVisual.State, PowerCellFourVisualStates.First);
                break;
            case <= 50f:
                _appearance.SetData(ent.Owner, PowerCellFourVisual.State, PowerCellFourVisualStates.Second);
                break;
            case <= 75f:
                _appearance.SetData(ent.Owner, PowerCellFourVisual.State, PowerCellFourVisualStates.Third);
                break;
            case <= 100f:
                _appearance.SetData(ent.Owner, PowerCellFourVisual.State, PowerCellFourVisualStates.Fourth);
                break;
        }
    }
}
