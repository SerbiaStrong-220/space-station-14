// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.DeviceLinking;
using Content.Shared.FixedPoint;
using Content.Shared.SS220.SupaKitchen;
using Content.Shared.SS220.SupaKitchen.Components;
using Robust.Shared.Audio;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.SupaKitchen.Components;

[RegisterComponent]
public sealed partial class OvenComponent : SharedOvenComponent
{
    [ViewVariables]
    public EntityUid? LastUser;

    [ViewVariables]
    public List<EntityUid> EntityPack = [];

    [ViewVariables]
    public Dictionary<string, FixedPoint2> ReagentsPack = [];

    [ViewVariables]
    public float PackCookingTime;

    [ViewVariables]
    public CookingRecipePrototype? CurrentCookingRecipe;

    [DataField]
    public string ContainerName = "oven_entity_container";

    [ViewVariables]
    public Container Container = default!;

    [DataField]
    public float HeatPerSecond = 100;

    [DataField]
    public float HeatingThreshold = 373.15f;

    #region  audio
    [DataField]
    public SoundSpecifier ActivateSound = new SoundPathSpecifier("/Audio/SS220/SupaKitchen/oven/oven_loop_start.ogg");
    [DataField]
    public SoundSpecifier FoodDoneSound = new SoundPathSpecifier("/Audio/SS220/SupaKitchen/ring.ogg");

    [DataField]
    public SoundSpecifier LoopingSound = new SoundPathSpecifier("/Audio/SS220/SupaKitchen/oven/oven_loop_mid.ogg");
    #endregion

    #region Sink ports
    [DataField]
    public ProtoId<SinkPortPrototype> OnPort = "On";

    [DataField]
    public ProtoId<SinkPortPrototype> TogglePort = "Toggle";
    #endregion
}
