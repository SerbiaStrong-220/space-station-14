// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;
using Content.Shared.Actions;

namespace Content.Shared.SS220.Clothing;

/// <summary>
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class InnerHandToggleProviderComponent : Component
{
    [ViewVariables]
    public Entity<InnerHandToggleableComponent>? InnerUser;

    [ViewVariables]
    public string? ContainerName;

    [ViewVariables]
    public string? HandName;
}

public sealed partial class ToggleInnerHandEvent : InstantActionEvent
{
    [DataField(required: true)]
    public string Hand;
}

public sealed class ProvideToggleInnerHandEvent(Entity<InnerHandToggleProviderComponent> hidable, string hand) : HandledEntityEventArgs
{
    public Entity<InnerHandToggleProviderComponent> Hidable = hidable;
    public string Hand = hand;
}
public sealed class RemoveToggleInnerHandEvent(Entity<InnerHandToggleProviderComponent> hidable, string handContainer) : HandledEntityEventArgs
{
    public Entity<InnerHandToggleProviderComponent> Hidable = hidable;
    public string HandContainer = handContainer;
}
