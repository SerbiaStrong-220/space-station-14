// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.Humanoid.Prototypes;

public sealed partial class SpeciesPrototype
{
    [DataField]
    public Dictionary<Sex, ProtoId<SkinColorationPrototype>> SexSkinColorations { get; private set; } = new();

    public ProtoId<SkinColorationPrototype> GetSkinColoration(Sex sex)
    {
        return SexSkinColorations.GetValueOrDefault(sex, SkinColoration);
    }
}
