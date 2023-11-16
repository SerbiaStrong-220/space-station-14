// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Medicine.Injury.Components;

namespace Content.Shared.SS220.Medicine.Injury;

[ByRefEvent]
public record struct InjuryAddedEvent(EntityUid injuryEntity, InjuryComponent injuryComponent, InjuriesContainerComponent injuriesContainerComponent);

[ByRefEvent]
public record struct InjuryRemovedEvent(EntityUid injuryEntity, InjuryComponent injuryComponent, InjuriesContainerComponent injuriesContainerComponent);

[ByRefEvent]
public record struct InjurySeverityStageChangedEvent(EntityUid injuryEntity, InjuryComponent injuryComponent, InjurySeverityStages OldInjuryStage, InjurySeverityStages newInjuryStage);
