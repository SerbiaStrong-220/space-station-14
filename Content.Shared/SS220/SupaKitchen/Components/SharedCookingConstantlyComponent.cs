// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.SupaKitchen.Components;

[NetworkedComponent]
public abstract partial class SharedCookingConstantlyComponent : Component
{
    #region State
    [ViewVariables]
    public CookingConstantlyState LastState = CookingConstantlyState.Idle;

    [ViewVariables]
    public CookingConstantlyState CurrentState
    {
        get => _currentState;
        set
        {
            LastState = _currentState;
            _currentState = value;
        }

    }

    [ViewVariables]
    public CookingConstantlyState _currentState = CookingConstantlyState.Idle;
    #endregion

    [DataField]
    public bool UseEntityStorage = true;

    public NetEntity? PlayingStream { get; set; }
}

[Serializable, NetSerializable]
public sealed class CookingConstantlyComponentState : ComponentState
{
    public CookingConstantlyState LastState;
    public CookingConstantlyState CurrentState;
    public NetEntity? PlayingStream;

    public CookingConstantlyComponentState(CookingConstantlyState lastState, CookingConstantlyState currentState, NetEntity? playingStream)
    {
        LastState = lastState;
        CurrentState = currentState;
        PlayingStream = playingStream;
    }
}

[Serializable, NetSerializable]
public enum CookingConstantlyState
{
    UnPowered,
    Idle,
    Active,
    Broken
}

[Serializable, NetSerializable]
public enum CookingConstantlyVisuals
{
    VisualState,
    Active,
    ActiveUnshaded
}
