using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.IdentityManagement.Components;
using Content.Shared.Overlays;
using Content.Shared.PDA;
using Content.Shared.Security.Components;
using Content.Shared.SS220.CriminalRecords;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays;

public sealed class ShowCriminalRecordIconsSystem : EquipmentHudSystem<ShowCriminalRecordIconsComponent>
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly IPrototypeManager _prototypeMan = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IdentityComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent); // 220 secincons fix
    }

    private void OnGetStatusIconsEvent(EntityUid uid, IdentityComponent _, ref GetStatusIconsEvent ev) // 220 secincons fix
    {
        if (!IsActive)
            return;

        //SS220 Criminal-Records begin

        // WizDen code, no longer needed
        // if (_prototype.TryIndex<StatusIconPrototype>(component.StatusIcon.Id, out var iconPrototype))
        //     ev.StatusIcons.Add(iconPrototype);

        string? securityRecordType = null;
        if (_accessReader.FindAccessItemsInventory(uid, out var items))
        {
            foreach (var item in items)
            {
                // ID Card
                if (TryComp(item, out IdCardComponent? id))
                {
                    securityRecordType = id.CurrentSecurityRecord?.RecordType; //SS220 Criminal-Records
                    break;
                }

                // PDA
                if (TryComp(item, out PdaComponent? pda)
                    && pda.ContainedId != null
                    && TryComp(pda.ContainedId, out id))
                {
                    securityRecordType = id.CurrentSecurityRecord?.RecordType; //SS220 Criminal-Records
                    break;
                }
            }
        }

        if (securityRecordType != null)
        {
            if (_prototypeMan.TryIndex<CriminalStatusPrototype>(securityRecordType, out var criminalStatus))
            {
                if (criminalStatus.StatusIcon.HasValue)
                {
                    if (_prototypeMan.TryIndex(criminalStatus.StatusIcon, out var secIcon))
                        ev.StatusIcons.Add(secIcon);
                    else
                        Log.Error($"Invalid security status icon prototype: {secIcon}");
                }
            }
            else
            {
                Log.Error($"Invalid security status prototype: {criminalStatus}");
            }
        }
        //SS220 Criminal-Records end
    }
}
