namespace Content.Shared.SS220.Cryostasis.Events;

[ByRefEvent]
public record struct ChangeInjectorDelayEvent(EntityUid Injector, EntityUid Target, EntityUid User, TimeSpan Delay);
