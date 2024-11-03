// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Robust.Shared.Prototypes;
using Robust.Shared.Audio;
using Content.Shared.Dataset;

namespace Content.Shared.SS220.Shout;
/// <summary>
/// Action raised to scream smth
/// </summary>
public sealed partial class ShoutActionEvent : InstantActionEvent
{
    /// <summary>
    /// Sound played when action button is pressed
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? ShoutSound;

    /// <summary>
    /// Shouted phrase when action button is pressed
    /// </summary>
    [DataField]
    public ProtoId<DatasetPrototype>? ShoutPhrases;
}
