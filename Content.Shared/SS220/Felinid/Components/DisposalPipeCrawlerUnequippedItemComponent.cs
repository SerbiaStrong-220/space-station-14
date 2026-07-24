namespace Content.Shared.SS220.Felinid.Components;

/// <summary>
/// Allows an item just unequipped by a pipecrawler to complete its predicted transfer into a hand.
/// </summary>
[RegisterComponent]
public sealed partial class DisposalPipeCrawlerUnequippedItemComponent : Component
{
    public EntityUid Pipecrawler;
}
