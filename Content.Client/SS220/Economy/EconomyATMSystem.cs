// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.SS220.Economy;
using Content.Shared.Tools.Components;

namespace Content.Client.SS220.Economy;

public sealed class EconomyATMSystem : SharedEconomyATMSystem
{
    protected override void OnEnterButtonPressed(Entity<EconomyATMComponent> ent, ref EconomyATMKeypadEnterMessage args)
    {
        // Client
    }

    protected override void OnInteractUsing(Entity<EconomyATMComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (TryComp<ToolComponent>(args.Used, out var tool) && Tool.HasQuality(args.Used, ent.Comp.ATMResetMethod, tool))
        {
            args.Handled = true;
            Tool.UseTool(args.Used, args.User, ent, ent.Comp.ATMResetDelay, ent.Comp.ATMResetMethod, new EconomyATMResetEvent(), toolComponent: tool);
        }
    }
    protected override void OnATMReset(Entity<EconomyATMComponent> ent, ref EconomyATMResetEvent args)
    {
        if (args.Cancelled)
            return;

        var locSelf = Loc.GetString("economy-atm-reset-self");
        var locOthers = Loc.GetString("economy-atm-reset-others", ("user", Identity.Name(args.User, EntityManager)));

        PopupSystem.PopupPredicted(locSelf, locOthers, ent, args.User);
    }
}
