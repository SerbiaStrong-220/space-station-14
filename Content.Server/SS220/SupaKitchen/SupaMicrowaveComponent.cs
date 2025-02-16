// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Destructible.Thresholds;
using Content.Shared.DeviceLinking;
using Content.Shared.SS220.SupaKitchen;
using Content.Shared.SS220.SupaKitchen.Components;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.SupaKitchen;

[RegisterComponent]
public sealed partial class SupaMicrowaveComponent : BaseCookingInstrumentComponent
{
    #region  stats
    [DataField]
    public uint MaxCookingTimer = 30;

    [DataField]
    public float TemperatureUpperThreshold = 373.15f;

    [DataField]
    public float HeatPerSecond = 100;

    [DataField]
    public int Capacity = 15;

    [DataField]
    public float CookTimeMultiplier = 1f;
    #endregion

    #region  state
    [ViewVariables]
    public uint CookingTimer = 0;

    [ViewVariables(VVAccess.ReadWrite)]
    public float CookTimeRemaining;

    [ViewVariables]
    public (CookingRecipePrototype?, int) CurrentlyCookingRecipe = (null, 0);

    [ViewVariables]
    public int CurrentCookTimeButtonIndex;

    [ViewVariables, Access(typeof(SupaMicrowaveSystem), Other = AccessPermissions.Read)]
    public SupaMicrowaveState CurrentState = SupaMicrowaveState.Idle;
    #endregion

    #region  audio
    [DataField]
    public SoundSpecifier BeginCookingSound = new SoundPathSpecifier("/Audio/Machines/microwave_start_beep.ogg");
    [DataField]
    public SoundSpecifier FoodDoneSound = new SoundPathSpecifier("/Audio/Machines/microwave_done_beep.ogg");
    [DataField]
    public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");
    [DataField]
    public SoundSpecifier ItemBreakSound = new SoundPathSpecifier("/Audio/Effects/clang.ogg");

    public EntityUid? PlayingStream { get; set; }
    [DataField]
    public SoundSpecifier LoopingSound = new SoundPathSpecifier("/Audio/Machines/microwave_loop.ogg");
    #endregion

    [DataField]
    public string StorageName = "supa_microwave_entity_container";

    public Container Storage = default!;

    #region Sink ports
    [DataField]
    public ProtoId<SinkPortPrototype> OnPort = "On";
    #endregion

    #region Danger
    /// <summary>
    /// How frequently the microwave can malfunction.
    /// </summary>
    [DataField]
    public MinMax MalfunctionInterval = new MinMax(1, 5);

    [ViewVariables]
    public TimeSpan MalfunctionTime = TimeSpan.Zero;

    /// <summary>
    /// Chance of an explosion occurring when we microwave a metallic object
    /// </summary>
    [DataField]
    public float ExplosionChance = .1f;

    /// <summary>
    /// Chance of lightning occurring when we microwave a metallic object
    [DataField]
    public float LightningChance = .75f;
    #endregion

    /// <summary>
    /// If this microwave can give ids accesses without exploding
    /// </summary>
    [DataField]
    public bool CanMicrowaveIdsSafely = true;

    /// <summary>
    /// Chance of an explosion occurring when we microwave a id card
    /// It's use if <see cref="CanMicrowaveIdsSafely"/> is false
    /// </summary>
    [DataField]
    public float IdCardExplosionChance = 1f;
}

public sealed class ProcessedInSupaMicrowaveEvent : HandledEntityEventArgs
{
    public EntityUid MachineEntity;
    public EntityUid? User;
    public EntityUid Item;

    public ProcessedInSupaMicrowaveEvent(EntityUid machineEntity, EntityUid item, EntityUid? user = null)
    {
        MachineEntity = machineEntity;
        User = user;
        Item = item;
    }
}
