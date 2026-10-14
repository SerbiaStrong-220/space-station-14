// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.SuperMatter.Ui;
using Robust.Shared.GameStates;

namespace Content.Client.SS220.SuperMatter.Observer;

/// <summary> We use this component to mark entities which can receiver </summary>
[RegisterComponent, NetworkedComponent] // Networked so Station AI gets them on the client when they are added at runtime
public sealed partial class SuperMatterObserverReceiverComponent : SharedSuperMatterObserverReceiverComponent;
