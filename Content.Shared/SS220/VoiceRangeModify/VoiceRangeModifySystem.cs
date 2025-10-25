using Content.Shared.Chat;

namespace Content.Shared.SS220.VoiceRangeModify;

public sealed class VoiceRangeModifySystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<VoiceRangeModifyComponent, VoiceModifyRangeEvent>(OnModifyVoiceRange);
        SubscribeLocalEvent<VoiceRangeModifyComponent, WhisperModifyRangeEvent>(OnModifyWhisperRange);
    }

    private void OnModifyVoiceRange(Entity<VoiceRangeModifyComponent> ent, ref VoiceModifyRangeEvent args)
    {
        args.VoiceRange = ent.Comp.VoiceRange;
    }

    private void OnModifyWhisperRange(Entity<VoiceRangeModifyComponent> ent, ref WhisperModifyRangeEvent args)
    {
        args.WhisperClearRange = ent.Comp.BaseWhisperClearRange;
        args.WhisperMuffledRange = ent.Comp.BaseWhisperMuffledRange;
    }
}

[ByRefEvent]
public record struct VoiceModifyRangeEvent(float VoiceRange);

[ByRefEvent]
public record struct WhisperModifyRangeEvent(float WhisperClearRange, float WhisperMuffledRange);
