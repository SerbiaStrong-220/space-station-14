namespace Content.Server.SS220.Bed.Cryostorage;

/// <summary>
/// Raised before body will deleted by a cryostorages
/// </summary>
[ByRefEvent]
public readonly record struct BeingCryoDeletedEvent();
