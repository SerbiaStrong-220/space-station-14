using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.TTS;

[Prototype]
public sealed partial class TtsVoiceCategoryPrototype : IPrototype, IEquatable<TtsVoiceCategoryPrototype>
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name = string.Empty;
    public string LocalizedName => Loc.GetString(Name);

    [DataField]
    public LocId Description = string.Empty;
    public string LocalizedDescription => string.IsNullOrEmpty(Description) ? string.Empty : Loc.GetString(Description);

    [DataField]
    public Color Color = Color.White.WithAlpha(0.375f);

    public override bool Equals(object? obj)
    {
        if (obj is not TtsVoiceCategoryPrototype other)
            return false;

        return Equals(other);
    }

    public bool Equals(TtsVoiceCategoryPrototype? other)
    {
        if (other == null)
            return false;

        return ID == other.ID;
    }

    public override int GetHashCode()
    {
        return ID.GetHashCode();
    }
}
