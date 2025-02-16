// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.SupaKitchen.Components;

[NetworkedComponent]
public abstract partial class SharedOvenComponent : BaseCookingInstrumentComponent
{
    #region State
    [ViewVariables]
    public OvenState LastState = OvenState.Idle;

    [ViewVariables]
    public OvenState CurrentState
    {
        get => _currentState;
        set
        {
            LastState = _currentState;
            _currentState = value;
        }

    }

    [ViewVariables]
    private OvenState _currentState = OvenState.Idle;
    #endregion

    [DataField]
    public bool UseEntityStorage = true;

    #region Audio
    public NetEntity? PlayingStream { get; set; }
    #endregion
}

[Serializable, NetSerializable]
public sealed class OvenComponentState : ComponentState
{
    public OvenState LastState;
    public OvenState CurrentState;
    public NetEntity? PlayingStream;

    public OvenComponentState(OvenState lastState, OvenState currentState, NetEntity? playingStream)
    {
        LastState = lastState;
        CurrentState = currentState;
        PlayingStream = playingStream;
    }
}

[Serializable, NetSerializable]
public enum OvenState
{
    UnPowered,
    Idle,
    Active,
    Broken
}

[Serializable, NetSerializable]
public enum OvenVisuals
{
    VisualState,
    Active,
    ActiveUnshaded
}
