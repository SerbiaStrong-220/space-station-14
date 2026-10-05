namespace Content.Shared.Storage.Events;

// SS220-felinid-pipecrawl
[ByRefEvent]
public record struct StorageInsertHeldItemAttemptEvent(EntityUid Storage, EntityUid Item)
{
    public bool BypassDropActionBlocker;
}
