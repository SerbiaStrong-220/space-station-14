using Content.Shared.SS220.SetSelector;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.SetSelector;

/// <summary>
///
/// </summary>
[RegisterComponent, Access(typeof(SetSelectorSystem))]
public sealed partial class SetSelectorComponent  : Component
{
    /// <summary>
    /// List of sets available for selection
    /// </summary>
    [DataField]
    public List<ProtoId<SelectableSetPrototype>> PossibleSets = new();

    [DataField]
    public List<int> SelectedSets = new();

    [DataField]
    public SoundCollectionSpecifier ApproveSound = new SoundCollectionSpecifier("storageRustle");

    /// <summary>
    /// Max number of sets you can select.
    /// </summary>
    [DataField]
    public int MaxSelectedSets = 2;

    /// <summary>
    /// Title field for selectable set ui.
    /// </summary>
    [DataField]
    public LocId ToolName = "set-selector-window-title";

    /// <summary>
    /// Description field for selectable set ui.
    /// </summary>
    [DataField]
    public LocId ToolDesc = "set-selector-window-description";

    /// <summary>
    /// What entity all the spawned items will appear inside of
    /// If null, will instead drop on the ground.
    /// </summary>
    [DataField]
    public EntProtoId? SpawnedStoragePrototype;
}
