using Robust.Shared.Audio; //SS220-NonTactile

namespace Content.Shared.Interaction.Events;

/// <summary>
/// Raised on the target when failing to pet/hug something.
/// </summary>
// TODO INTERACTION
// Rename this, or move it to another namespace to make it clearer that this is specific to "petting/hugging" (InteractionPopupSystem)
[ByRefEvent]
//SS220-NonTactile begin
public record struct InteractionFailureEvent(EntityUid User)
{
    public string? Message;
    public SoundSpecifier? Sound;
    public string? MessagePerceivedByOthers;
}
//SS220-NonTactile end

