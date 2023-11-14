// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Medicine.Injure.Components;

namespace Content.Shared.SS220.Medicine.Injure;

[ByRefEvent]
public record struct InjureAddedEvent(EntityUid InjureEntity, InjureComponent InjureComponent, InjuredComponent InjuredComponent);

[ByRefEvent]
public record struct InjureRemovedEvent(EntityUid InjureEntity, InjureComponent InjureComponent, InjuredComponent InjuredComponent);

[ByRefEvent]
public record struct InjureStageChangedEvent(EntityUid InjureEntity, InjureComponent InjureComponent, InjuredComponent InjuredComponent, InjureStages OldInjureStage, InjureStages NewInjureStage);