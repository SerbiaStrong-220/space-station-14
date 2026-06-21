// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Server.SS220.Language;
using Content.Shared.SS220.Mech.Components;
using Content.Shared.SS220.Mech.Systems;
using Content.Shared.Inventory;
using Content.Shared.Radio.Components;
using Content.Shared.SS220.Language.Components;
using System.Linq;

namespace Content.Server.SS220.Mech.Systems;

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

        if (mechComp.PilotSlot.ContainedEntity is not { Valid: true } pilotValidated)
            return;

        var rider = EnsureComp<AltMechPilotComponent>(pilotValidated);

        if (TryComp<ActiveRadioComponent>(ent.Owner, out var mechRadio))
        {
            if (TryComp<InventoryComponent>(pilotValidated, out var pilotInventory) && _inventory.TryGetSlotContainer(pilotValidated, "ears", out var slot, out var def))
            {
                if (!TryComp<ActiveRadioComponent>(slot.ContainedEntity, out var radioComp))
                    return;

                mechRadio.FrequencyChannels = radioComp.FrequencyChannels;
            }

            if (TryComp<ActiveRadioComponent>(pilotValidated, out var embeddedRadio))//in case the pilot is a radio himself
            {
                foreach (var channel in embeddedRadio.Channels)
                    mechRadio.FrequencyChannels = embeddedRadio.FrequencyChannels;
            }
        }

        if (TryComp<LanguageComponent>(ent.Owner, out var mechLanguage) && TryComp<LanguageComponent>(pilotValidated, out var pilotLanguage))
            _language.AddLanguages((ent.Owner, mechLanguage), pilotLanguage.AvailableLanguages.ToList());
    }

    private void OnMechExited(Entity<VoiceInheritorComponent> ent, ref OnMechExitEvent args)
    {
        if (TryComp<ActiveRadioComponent>(ent.Owner, out var mechRadio))
            mechRadio.FrequencyChannels.Clear();

        if (TryComp<LanguageComponent>(ent.Owner, out var mechLanguage))
            _language.ClearLanguages((ent.Owner, mechLanguage));
    }
}
