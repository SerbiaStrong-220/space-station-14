// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Atmos;

namespace Content.Server.SS220.Atmos;

[RegisterComponent, Access(typeof(ItemGasSpawnerSystem))]
public sealed partial class ItemGasSpawnerComponent : Component
{
    /// <summary>
    ///      If the number of moles in the external environment exceeds this number, no gas will be spawned.
    /// </summary>
    [DataField]
    public float MaxExternalAmount = float.PositiveInfinity;

    /// <summary>
    ///      If the pressure (in kPA) of the external environment exceeds this number, no gas will be spawned.
    /// </summary>
    [DataField]
    public float MaxExternalPressure = 101.325f;

    /// <summary>
    ///     Gas to spawn.
    /// </summary>
    [DataField(required: true)]
    public Gas SpawnGas;

    /// <summary>
    ///     Temperature in Kelvin.
    /// </summary>
    [DataField]
    public float SpawnTemperature = Atmospherics.T20C;

    /// <summary>
    ///     Number of moles created per second.
    /// </summary>
    [DataField]
    public float SpawnAmount = Atmospherics.MolesCellStandard * 20f;

    /// <summary>
    ///     Is spawn limited?
    /// </summary>
    [DataField]
    public bool LimitedSpawn = false;

    /// <summary>
    ///     How much moles of gas can be spawned.
    ///     Didn't decrease if <see cref="LimitedSpawn"/> is false.
    /// </summary>
    [DataField("amountToSpawn")]
    public float RemainingAmountToSpawn = 500f;
}
