using Content.Server.SS220.DarkForces.Narsi.Buildings.Altar.Rituals.Base;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.DarkForces.Narsi.Buildings.Altar.Rituals.Prototypes;

[Prototype]
public sealed partial class NarsiRitualPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true, serverOnly: true)]
    public string Description = default!;

    [DataField(required: true, serverOnly: true)]
    public int Duration;

    [DataField(required: true, serverOnly: true)]
    public NarsiRitualEffect Effect = default!;

    [DataField(required: true, serverOnly: true)]
    public string Name = default!;

    [DataField(required: true, serverOnly: true)]
    public string RequirementsDesc = default!;

    [DataField]
    public SoundSpecifier Sound = new SoundCollectionSpecifier("Rituals");

    [DataField]
    public AudioParams SoundParams = AudioParams.Default.WithVolume(0.25f);

    [DataField(required: true)]
    public NarsiRitualRequirements Requirements = default!;
}
