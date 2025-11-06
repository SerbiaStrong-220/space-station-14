using Content.Shared.Atmos;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Vape;

[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class VapeComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public TimeSpan MaxPuffTime = TimeSpan.FromSeconds(5);

    [DataField]
    [AutoNetworkedField]
    public EntityUid? SoundEntity;

    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new Dictionary<string, FixedPoint2>
        {
            { "Heat", 0.24},
        },
    };

    [DataField]
    [AutoNetworkedField]
    public EntityUid? User;

    [DataField]
    [AutoNetworkedField]
    public bool Puffing;

    [DataField]
    public SoundSpecifier VapingSound = new SoundPathSpecifier("/Audio/SS220/Items/Vape/vaping_1.ogg", AudioParams.Default.WithLoop(true).WithVolume(-2f));

    [DataField]
    [AutoNetworkedField]
    public TimeSpan? StartPuffingTime;

    [DataField]
    [AutoNetworkedField]
    public EntityUid? AtomizerEntity;

    [DataField]
    [AutoNetworkedField]
    public EntityUid? CartridgeEntity;

    [DataField]
    [AutoNetworkedField]
    public bool IsEmagged;

    [DataField]
    [AutoNetworkedField]
    public float AccumulatedVapedVolume;

    [DataField]
    public Gas GasType = Gas.WaterVapor;

    /// <summary>
    /// Maximum volume of <see cref="GasType"/>, that spawn while unequipped
    /// </summary>
    [DataField]
    public float MaxVaporVolume = 20f;

    /// <summary>
    /// Scaled volume <see cref="GasType"/>, that indicates,
    /// in what coefficient should divide <see cref="AccumulatedVapedVolume"/>
    /// to prevent a lot of <see cref="GasType"/>
    /// </summary>
    [DataField]
    public float ScaledVaporVolume = 10f;
}
