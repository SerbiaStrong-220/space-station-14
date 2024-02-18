// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Serialization;
namespace Content.Shared.SS220.Speech;

/// <summary>
/// Marks, if this item has the VocalComponent
/// </summary>

[RegisterComponent]
public sealed partial class SpecialSoundsComponent : Component
{
    [ByRefEvent]
    public readonly record struct InitSpecialSoundsEvent();

    [ByRefEvent]
    public readonly record struct UnloadSpecialSoundsEvent();

    /// <summary>
    ///     Current sensor mode. Can be switched by user verbs.
    /// </summary>
    [DataField("mode")]
    public SpecialSoundMode Mode = SpecialSoundMode.SpecialSoundOn;
}

public sealed class InitSpecialSoundsEvent : EntityEventArgs
{
    public EntityUid Item;

    public InitSpecialSoundsEvent(EntityUid item)
    {
        Item = item;
    }
}

public sealed class UnloadSpecialSoundsEvent : EntityEventArgs
{
    public EntityUid Item;
    public UnloadSpecialSoundsEvent(EntityUid item)
    {
        Item = item;
    }
}

[Serializable, NetSerializable]
public enum SpecialSoundMode : byte
{
    SpecialSoundOff = 0,

    SpecialSoundOn = 1
}
