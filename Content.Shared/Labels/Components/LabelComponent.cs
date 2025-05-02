using Content.Shared.Labels.EntitySystems;
using Content.Shared.SS220.Photocopier;
using Robust.Shared.GameStates;

namespace Content.Shared.Labels.Components;

/// <summary>
/// Makes entities have a label in their name. Labels are normally given by <see cref="HandLabelerComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LabelComponent : Component, IPhotocopyableComponent
{
    /// <summary>
    /// Current text on the label. If set before map init, during map init this string will be localized.
    /// This permits localized preset labels with fallback to the text written on the label.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? CurrentLabel { get; set; }

    public IPhotocopiedComponentData GetPhotocopiedData()
    {
        return new LabelComponentPhotocopiedData()
        {
            CurrentLabel = CurrentLabel
        };
    }
}

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
            var labelSys = entSys.System<SharedLabelSystem>();
            labelSys.Label(uid, CurrentLabel, label: labelComponent);
        }
    }
}
