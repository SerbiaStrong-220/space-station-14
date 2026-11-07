using Content.Shared.Alert;
using Content.Shared.Damage;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Cuffs.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedCuffableSystem))]
public sealed partial class CuffableComponent : Component
{
    /// <summary>
    /// The current RSI for the handcuff layer
    /// </summary>
    [DataField("currentRSI"), ViewVariables(VVAccess.ReadWrite)]
    public string? CurrentRSI;

    /// <summary>
    /// How many of this entity's hands are currently cuffed.
    /// </summary>
    //SS220-ArahnidHandReturn begin
    [DataField]
    public int HandsPerCuff = 2;

    [ViewVariables]
    public int CuffedHandCount => Container.ContainedEntities.Count * HandsPerCuff;
    //SS220-ArahnidHandReturn end

    /// <summary>
    ///     Container of various handcuffs currently applied to the entity.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Container Container = default!;

    /// <summary>
    /// Whether or not the entity can still interact (is not cuffed)
    /// </summary>
    [DataField("canStillInteract"), ViewVariables(VVAccess.ReadWrite)]
    public bool CanStillInteract = true;

    [DataField]
    public ProtoId<AlertPrototype> CuffedAlert = "Handcuffed";
}

public sealed partial class RemoveCuffsAlertEvent : BaseAlertEvent;

[Serializable, NetSerializable]
public sealed class CuffableComponentState : ComponentState
{
    public readonly bool CanStillInteract;
    public readonly int NumHandsCuffed;
    public readonly string? RSI;
    public readonly string? IconState;
    public readonly Color? Color;

    public CuffableComponentState(int numHandsCuffed, bool canStillInteract, string? rsiPath, string? iconState, Color? color)
    {
        NumHandsCuffed = numHandsCuffed;
        CanStillInteract = canStillInteract;
        RSI = rsiPath;
        IconState = iconState;
        Color = color;
    }
}

[ByRefEvent]
public readonly record struct CuffedStateChangeEvent;

