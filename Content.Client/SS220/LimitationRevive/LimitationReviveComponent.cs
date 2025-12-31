// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.LimitationRevive;
using Robust.Shared.GameStates;

namespace Content.Client.SS220.LimitationRevive;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class LimitationReviveComponent : SharedLimitationReviveComponent
{
}
