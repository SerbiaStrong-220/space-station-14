// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.SupaKitchen;

[Prototype("cookingInstrumentType")]
public sealed class CookingInstrumentTypePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; } = default!;

    [DataField("name")]
    private string _name = string.Empty;

    public string Name => Loc.GetString(_name);

    [DataField]
    public string? IconPath;
}
