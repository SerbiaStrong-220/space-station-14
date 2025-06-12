// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Undereducated
{
    [Prototype("languageReplacements")]
    public sealed partial class LanguageReplacementsPrototype : IPrototype
    {
        [IdDataField]
        public string ID { get; private set; } = default!;

        [DataField]
        public Dictionary<string, string> Replacements = new();
    }
}
