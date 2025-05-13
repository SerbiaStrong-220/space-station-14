using Content.Client.Implants.UI;
using Content.Client.Items;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Implants;

public sealed class ImplanterSystem : SharedImplanterSystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ImplanterComponent, AfterAutoHandleStateEvent>(OnHandleImplanterState);
        Subs.ItemStatus<ImplanterComponent>(ent => new ImplanterStatusControl(ent));
    }

    private void OnHandleImplanterState(EntityUid uid, ImplanterComponent component, ref AfterAutoHandleStateEvent args)
    {
        if (_uiSystem.TryGetOpenUi<DeimplantBoundUserInterface>(uid, DeimplantUiKey.Key, out var bui))
        {
            Dictionary<string, string> implants = new();
            foreach (var implant in component.DeimplantWhitelist)
            {
                if (_proto.TryIndex(implant, out var proto))
                // SS220-implant-name-fix-begin
                {
                    var locData = Loc.GetEntityData(proto.ID);
                    var name = locData.Attributes.FirstOrNull(x => x.Key == "true-name")?.Value ??
                                string.Join(" ", proto.Name, locData.Suffix);
                    implants.Add(proto.ID, name);
                }
                // SS220-implant-name-fix-end
            }

            bui.UpdateState(implants, component.DeimplantChosen);
        }

        component.UiUpdateNeeded = true;
    }
}
