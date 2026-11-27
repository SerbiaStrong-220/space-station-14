using Robust.Shared.Audio;

namespace Content.Shared.Interaction.Events;

/// <summary>
/// Raised on the target when successfully petting/hugging something.
/// </summary>
// TODO INTERACTION
// Rename this, or move it to another namespace to make it clearer that this is specific to "petting/hugging" (InteractionPopupSystem)
[ByRefEvent]
//SS220-NonTactile begin
public record struct InteractionSuccessEvent(EntityUid User)
{
    public LocId? Message;
    public SoundSpecifier? Sound;
    public LocId? MessagePerceivedByOthers;
}
//SS220-NonTactile end

