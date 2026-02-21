// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.SS220.Language;
using Content.Shared.FCB.Mech.Components;
using Content.Shared.FCB.Mech.Systems;
using Content.Shared.Inventory;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.Radio.Components;
using Content.Shared.SS220.Language.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.SS220.Mech;

public sealed class VoiceInheritorSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly LanguageSystem _language = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<VoiceInheritorComponent, OnMechEntryEvent>(OnMechEntry);
        SubscribeLocalEvent<VoiceInheritorComponent, OnMechExitEvent>(OnMechExited);
    }

    private void OnMechEntry(Entity<VoiceInheritorComponent> ent, ref OnMechEntryEvent args)
    {
        if (!TryComp<AltMechComponent>(ent.Owner, out var mechComp))
            return;

        if (mechComp.PilotSlot.ContainedEntity == null)
            return;

        EntityUid pilot = (EntityUid)mechComp.PilotSlot.ContainedEntity;

        var rider = EnsureComp<AltMechPilotComponent>(pilot);

        if (TryComp<ActiveRadioComponent>(ent.Owner, out var mechRadio))
        {
            if (TryComp<InventoryComponent>(pilot, out var pilotInventory) && _inventory.TryGetSlotContainer(pilot, "ears", out var slot, out var def))
            {
                if (!TryComp<ActiveRadioComponent>(slot.ContainedEntity, out var radioComp))
                    return;
                mechRadio.FrequencyChannels = radioComp.FrequencyChannels;
            }
            if (TryComp<ActiveRadioComponent>(pilot, out var embeddedRadio))//in case the pilot is a radio himself
            {
                foreach (var channel in embeddedRadio.Channels)
                    mechRadio.FrequencyChannels = embeddedRadio.FrequencyChannels;
            }
        }

        if(TryComp<LanguageComponent>(ent.Owner, out var mechLanguage) && TryComp<LanguageComponent>(pilot, out var pilotLanguage))
        {
            _language.AddLanguages((ent.Owner, mechLanguage), pilotLanguage.AvailableLanguages.ToList());
        }
    }

    private void OnMechExited(Entity<VoiceInheritorComponent> ent, ref OnMechExitEvent args)
    {
        if (TryComp<ActiveRadioComponent>(ent.Owner, out var mechRadio))
            mechRadio.FrequencyChannels.Clear();

        if (TryComp<LanguageComponent>(ent.Owner, out var mechLanguage))
            _language.ClearLanguages((ent.Owner, mechLanguage));
    }
}
