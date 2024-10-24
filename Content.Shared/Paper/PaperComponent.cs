using Content.Shared.SS220.Photocopier;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Paper;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PaperComponent : Component, IPhotocopyableComponent
{
    public PaperAction Mode;
    [DataField("content"), AutoNetworkedField]
    public string Content { get; set; } = "";

    /// <summary>
    ///     Allows to forbid to write on paper without using stamps as a hack
    /// </summary>
    [DataField("writable")]
    public bool Writable { get; set; } = true;

    [DataField("contentSize")]
    public int ContentSize { get; set; } = 6000;

    [DataField("stampedBy"), AutoNetworkedField]
    public List<StampDisplayInfo> StampedBy { get; set; } = new();

    /// <summary>
    ///     Stamp to be displayed on the paper, state from bureaucracy.rsi
    /// </summary>
    [DataField("stampState"), AutoNetworkedField]
    public string? StampState { get; set; }

    [DataField, AutoNetworkedField]
    public bool EditingDisabled = false;

    /// <summary>
    /// Sound played after writing to the paper.
    /// </summary>
    [DataField("sound")]
    public SoundSpecifier? Sound { get; private set; } = new SoundCollectionSpecifier("PaperScribbles", AudioParams.Default.WithVariation(0.1f));

    //SS220 Add auto form
    [AutoNetworkedField]
    public EntityUid? Writer;
    //SS220 Add auto form

    public IPhotocopiedComponentData GetPhotocopiedData()
    {
        return new PaperPhotocopiedData()
        {
            Content = Content,
            EditingDisabled = EditingDisabled,
            ContentSize = ContentSize,
            StampedBy = StampedBy,
            StampState = StampState
        };
    }

    [Serializable, NetSerializable]
    public sealed class PaperBoundUserInterfaceState : BoundUserInterfaceState
    {
        public readonly string Text;
        public readonly List<StampDisplayInfo> StampedBy;
        public readonly PaperAction Mode;

        public PaperBoundUserInterfaceState(string text, List<StampDisplayInfo> stampedBy, PaperAction mode = PaperAction.Read)
        {
            Text = text;
            StampedBy = stampedBy;
            Mode = mode;
        }
    }

    [Serializable, NetSerializable]
    public sealed class PaperInputTextMessage : BoundUserInterfaceMessage
    {
        public readonly string Text;

        public PaperInputTextMessage(string text)
        {
            Text = text;
        }
    }

    [Serializable, NetSerializable]
    public enum PaperUiKey
    {
        Key
    }

    [Serializable, NetSerializable]
    public enum PaperAction
    {
        Read,
        Write,
    }

    [Serializable, NetSerializable]
    public enum PaperVisuals : byte
    {
        Status,
        Stamp
    }

    [Serializable, NetSerializable]
    public enum PaperStatus : byte
    {
        Blank,
        Written
    }
}

[Serializable]
public sealed class PaperPhotocopiedData : IPhotocopiedComponentData
{
    [Dependency, NonSerialized] private readonly IEntitySystemManager _sysMan = default!;

    public PaperPhotocopiedData()
    {
        IoCManager.InjectDependencies(this);
    }

    public string? Content;
    public bool? EditingDisabled;
    public int? ContentSize;
    public List<StampDisplayInfo>? StampedBy;
    public string? StampState;

    public void RestoreFromData(EntityUid uid, Component someComponent)
    {
        var paperSystem = _sysMan.GetEntitySystem<PaperSystem>();

        if (someComponent is not PaperComponent paperComponent)
            return;

        if (ContentSize is { } contentSize)
            paperComponent.ContentSize = contentSize;

        var entity = new Entity<PaperComponent>(uid, paperComponent);

        //Don't set empty content string so empty paper notice is properly displayed
        if (!string.IsNullOrEmpty(Content))
            paperSystem.SetContent(entity, Content);

        if (EditingDisabled is { } editingDisabled)
            paperComponent.EditingDisabled = editingDisabled;

        // Apply stamps
        if (StampState is null || StampedBy is null)
            return;

        foreach (var stampedBy in StampedBy)
        {
            paperSystem.TryStamp(entity, stampedBy, StampState);
        }
    }
}
