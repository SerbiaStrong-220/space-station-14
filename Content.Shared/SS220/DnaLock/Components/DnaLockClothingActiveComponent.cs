// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.DnaLock.Components;

/// <summary>
/// Runtime component added to clothing when an unauthorized user equips it.
/// Stores who equipped the item and when the detonation should happen.
/// </summary>
[RegisterComponent]
public sealed partial class DnaLockClothingActiveComponent : Component
{
    /// <summary>
    /// EntityUid of the unauthorized wearer.
    /// </summary>
    public EntityUid WearerUid;

    /// <summary>
    /// Game time when the item should detonate.
    /// </summary>
    public TimeSpan ExplodeAt;

    /// <summary>
    /// Game time for the next beep/popup warning.
    /// </summary>
    public TimeSpan NextBeepAt;
}

