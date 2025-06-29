// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Actions;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;

namespace Content.Shared.SS220.DarkReaper;

public sealed partial class ReaperRoflEvent : InstantActionEvent
{
}

public sealed partial class ReaperStunEvent : InstantActionEvent
{
}

public sealed partial class ReaperConsumeEvent : EntityTargetActionEvent
{
}

public sealed partial class ReaperMaterializeEvent : InstantActionEvent
{
}

public sealed partial class ReaperSpawnEvent : InstantActionEvent
{
}

public sealed partial class ReaperSmokeEvent : InstantActionEvent
{
}

public sealed partial class ReaperBloodMistEvent : InstantActionEvent
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    /// BLOOD MIST

    /// <summary>
    /// How long the mist stays for, after it has spread
    /// </summary>
    [DataField]
    public TimeSpan BloodMistLength = TimeSpan.FromSeconds(10);
    /// <summary>
    /// Proto of what is being spawned by ability
    /// </summary>
    [DataField]
    public string BloodMistProto = "BloodMistSpread";

    /// <summary>
    /// BloodMist sound
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier BloodMistSound = new SoundPathSpecifier("/Audio/Items/smoke_grenade_smoke.ogg", new()
    {
        MaxDistance = 7
    });
    
}