using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Felinid.Components;

/// <summary>
/// Marks ordinary disposal contents so a crawling felinid can see them inside the pipe network.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DisposalPipeCrawlerContentsComponent : Component;
