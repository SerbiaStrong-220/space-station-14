using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Felinid.Components;

/// <summary>
/// Replicates the presence of a disposal tube to clients so pipe crawlers can render its interior.
/// The server-only <c>DisposalTubeComponent</c> remains the source of disposal behaviour.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DisposalPipeCrawlerTubeComponent : Component;
