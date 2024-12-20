// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;


namespace Content.Shared.SS220.Language;

/// <summary>
///     A component that allows an entity to speak and understand languages.
///     Language prototypes are taken from YML of <see cref="LanguagesPrototype"/>
///     The absence of this component gives the entity “Universal” language
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class LanguageComponent : Component
{
    /// <summary>
    ///  Selected language in which the entity will speak.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public string? CurrentLanguage = default!;

    /// <summary>
    ///  List of languages that the Entity speaks and understands.
    /// </summary>
    [DataField("learnedLanguages"), AutoNetworkedField]
    public List<ProtoId<LanguagesPrototype>> LearnedLanguages { get; set; } = new();
}
