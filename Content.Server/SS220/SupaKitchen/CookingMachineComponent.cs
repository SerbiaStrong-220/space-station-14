// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Construction.Prototypes;
using Content.Shared.SS220.SupaKitchen;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.SupaKitchen;

[RegisterComponent]
public sealed partial class CookingMachineComponent : Component
{
    #region  stats
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public uint MaxCookingTimer = 30;

    [DataField]
    public float TemperatureUpperThreshold = 373.15f;

    [DataField]
    public float HeatPerSecond = 100;
    #endregion

    #region  upgrades
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float CookTimeMultiplier = 1;
    //[DataField]
    //public ProtoId<MachinePartPrototype> MachinePartCookTimeMultiplier = "Capacitor";
    [DataField]
    public float CookTimeScalingConstant = 0.5f;
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

    [DataField, ViewVariables]
    public bool Broken = false;

    [DataField, ViewVariables, Access(typeof(CookingMachineSystem), Other = AccessPermissions.Read)]
    public bool Active = false;
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
    public bool AltActivationUI = false;
    [DataField]
    public bool UseEntityStorage = false;

    public Container Storage = default!;
}

public sealed class ProcessedInCookingMachineEvent : HandledEntityEventArgs
{
    public EntityUid MachineEntity;
    public CookingMachineComponent CookingMachine;
    public EntityUid? User;
    public EntityUid Item;

    public ProcessedInCookingMachineEvent(EntityUid machineEntity, CookingMachineComponent cookingMachine, EntityUid item, EntityUid? user = null)
    {
        MachineEntity = machineEntity;
        CookingMachine = cookingMachine;
        User = user;
        Item = item;
    }
}

public sealed class BeforeCookingMachineFinished : EntityEventArgs
{
    public CookingMachineComponent CookingMachine;

    public BeforeCookingMachineFinished(CookingMachineComponent component)
    {
        CookingMachine = component;
    }
}
