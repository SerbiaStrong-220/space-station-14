// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;

namespace Content.Shared.SS220.NonTactile;

public sealed partial class NonTactileSystem : EntitySystem
{
    private const string FailureString = "hugging-failure-generic-non-tactile";
    private const string OthersMessage = "hugging-failure-generic-non-tactile-others";

    [Dependency] private InteractionPopupSystem _interactionPopup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NonTactileComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<NonTactileComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<NonTactileComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<InteractionPopupComponent>(ent, out var popup))
            return;

        ent.Comp.OriginalSuccessChance = popup.SuccessChance;
        ent.Comp.OriginalFailureString = popup.InteractFailureString;
        ent.Comp.OriginalMessagePerceivedByOthers = popup.MessagePerceivedByOthers;
        ent.Comp.PopupOverridden = true;

        var entity = (ent.Owner, popup);

        _interactionPopup.SetSuccessChance(entity, 0f);
        _interactionPopup.SetInteractFailureString(entity, FailureString);
        _interactionPopup.SetMessagePerceivedByOthers(entity, OthersMessage);
    }

    private void OnShutdown(Entity<NonTactileComponent> ent, ref ComponentShutdown args)
    {
        if (!ent.Comp.PopupOverridden)
            return;

        ent.Comp.PopupOverridden = false;

        if (!TryComp<InteractionPopupComponent>(ent, out var popup))
            return;

        var entity = (ent.Owner, popup);

        _interactionPopup.SetSuccessChance(entity, ent.Comp.OriginalSuccessChance);
        _interactionPopup.SetInteractFailureString(entity, ent.Comp.OriginalFailureString);
        _interactionPopup.SetMessagePerceivedByOthers(entity, ent.Comp.OriginalMessagePerceivedByOthers);
    }
}

