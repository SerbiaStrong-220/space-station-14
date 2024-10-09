using Content.Server.SS220.DarkForces.Narsi.Buildings.Altar.Rituals.Base;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.DarkForces.Narsi.Buildings.Altar.Rituals.Prototypes;

[Prototype]
public sealed class NarsiRitualPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    [DataField(required: true, serverOnly: true)]
    public readonly string Description = default!;

    [DataField(required: true, serverOnly: true)]
    public readonly int Duration;

    [DataField(required: true, serverOnly: true)]
    public readonly NarsiRitualEffect Effect = default!;

    [DataField(required: true, serverOnly: true)]
    public readonly string Name = default!;

    [DataField(required: true, serverOnly: true)]
    public readonly string RequirementsDesc = default!;

    [DataField]
    public SoundSpecifier Sound = new SoundCollectionSpecifier("Rituals");

    [DataField]
    public AudioParams SoundParams = AudioParams.Default.WithVolume(0.25f);

    [DataField(required: true)]
    public readonly NarsiRitualRequirements Requirements = default!;
}
