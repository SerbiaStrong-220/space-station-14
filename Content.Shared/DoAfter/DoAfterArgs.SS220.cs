namespace Content.Shared.DoAfter;

public sealed partial class DoAfterArgs
{
    public DoAfterArgFlags ArgFlags = DoAfterArgFlags.None;
}

[Flags]
public enum DoAfterArgFlags : byte
{
    None = 0,

    IgnoreTraitsModification = 1 << 1,
    IgnoreExperienceModification = 1 << 2
}
