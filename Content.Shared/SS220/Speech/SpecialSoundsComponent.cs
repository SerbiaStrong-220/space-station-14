namespace Content.Shared.SS220.Speech;

/// <summary>
/// Marks, that this item has the VocalComponent
/// </summary>

[RegisterComponent]
public sealed partial class SpecialSoundsComponent : Component
{
    [ByRefEvent]
    public readonly record struct HasSpecialSoundsEvent();
}

public sealed class HasSpecialSoundsEvent : EntityEventArgs
{
    public EntityUid Item;

    public HasSpecialSoundsEvent(EntityUid item)
    {
        Item = item;
    }
}

public sealed class UnloadSpecialSoundsEvent : EntityEventArgs
{
    public UnloadSpecialSoundsEvent()
    {

    }
}
