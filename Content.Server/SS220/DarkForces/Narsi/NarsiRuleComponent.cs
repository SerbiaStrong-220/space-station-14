using Robust.Shared.Audio;

namespace Content.Server.SS220.DarkForces.Narsi;

[RegisterComponent]
[Access(typeof(NarsiRuleSystem))]
public sealed partial class NarsiRuleComponent : Component
{
    [DataField]
    public WinState WinStateStatus = WinState.Idle;

    [DataField]
    public EntityUid RuneSource = EntityUid.Invalid;

    // Welp, it isn't used anywhere for some reason
    //[DataField]
    //public SoundSpecifier NarsiExileSound = new SoundPathSpecifier("/Audio/SS220/DarkForces/Cult/narsi_destroy.ogg");

    // [DataField]
    // public SoundSpecifier NarsiSummonSound = new SoundPathSpecifier("/Audio/SS220/DarkForces/Cult/narsi_summon.ogg");

    public TimeSpan NarsiRepeatSoundAt = TimeSpan.Zero;
    public TimeSpan RoundEndAt = TimeSpan.Zero;
}

public enum WinState
{
    Idle,
    NarsiSummoning,
    NarsiLastStand,
    CultistWon
}
