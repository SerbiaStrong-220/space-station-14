using Content.Shared.Labels.EntitySystems;
using Content.Shared.SS220.Photocopier;
using Robust.Shared.GameStates;

namespace Content.Shared.Labels.Components;

/// <summary>
/// Makes entities have a label in their name. Labels are normally given by <see cref="HandLabelerComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LabelComponent : Component, IPhotocopyableComponent /* SS220 Photocopy */
{
    /// <summary>
    /// Current text on the label.
    /// Do not use this in entity prototypes - use <see cref="LocalizedLabel"/> instead.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? CurrentLabel { get; set; }

    /// <summary>
    /// Localization ID used to set <see cref="CurrentLabel"/> on map init.
    /// Use this in entity prototypes.
    /// </summary>
    [DataField]
    public LocId? LocalizedLabel { get; set; }

    /// <summary>
    /// Should the label show up in the examine menu?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Examinable = true;

    // SS220 Photocopy begin
    public IPhotocopiedComponentData GetPhotocopiedData()
    {
        return new LabelComponentPhotocopiedData()
        {
            CurrentLabel = CurrentLabel
        };
    }
    // SS220 Photocopy end
}

// SS220 Photocopy begin
[Serializable]
public sealed class LabelComponentPhotocopiedData : IPhotocopiedComponentData
{
    public string? CurrentLabel;
    public void RestoreFromData(EntityUid uid, Component someComponent)
    {
        if (someComponent is not LabelComponent labelComponent)
            return;

        if (CurrentLabel is not null)
        {
            var changed = CurrentLabel != labelComponent.CurrentLabel;
            if (!changed)
                return;

            var entSys = IoCManager.Resolve<IEntityManager>();
            var labelSys = entSys.System<LabelSystem>();
            labelSys.Label(uid, CurrentLabel, label: labelComponent);
        }
    }
}
// SS220 Photocopy end
