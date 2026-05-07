using Robust.Shared.Audio;

namespace Content.Server.SingleTouchBox.Components;

[RegisterComponent]
public sealed partial class SingleTouchBoxComponent : Component
{
    public HashSet<EntityUid> UsedBy = new();
    public NetEntity? ComboUser;
    public int InteractionCount;

    [DataField]
    public int MaxInteractionCount = 10;

    [DataField]
    public SoundSpecifier DevourSound =
        new SoundPathSpecifier("/Audio/Effects/demon_consume.ogg");
}
