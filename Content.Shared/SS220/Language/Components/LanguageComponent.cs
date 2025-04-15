// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Language.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Language.Components;

/// <summary>
///     A component that allows an entity to speak and understand languages.
///     Language prototypes are taken from YML of <see cref="LanguagePrototype"/>
///     The absence of this component gives the entity “Universal” language
/// </summary>
[RegisterComponent, Access(typeof(SharedLanguageSystem), Other = AccessPermissions.Read)]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LanguageComponent : Component
{
    /// <summary>
    ///  Selected language in which the entity will speak.
    ///  If null, the universal language will be used.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<LanguagePrototype>? SelectedLanguage;

    /// <summary>
    ///  List of languages that the Entity can speak.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<LanguageDefinition> AvailableLanguages = new();

    [DataField, AutoNetworkedField]
    public bool KnowAllLLanguages;
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class LanguageDefinition
{
    [DataField(required: true)]
    public ProtoId<LanguagePrototype> Id;

    [DataField]
    public bool CanSpeak = true;

    public LanguageDefinition(ProtoId<LanguagePrototype> id, bool canSpeak)
    {
        Id = id;
        CanSpeak = canSpeak;
    }

    public bool IsEqual(LanguageDefinition other)
    {
        return Id == other.Id;
    }
}
