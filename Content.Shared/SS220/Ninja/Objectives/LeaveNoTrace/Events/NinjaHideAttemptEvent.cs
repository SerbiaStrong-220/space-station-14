// Original code licensed under Imperial CLA.
// Copyright holders: orix0689 (discord) and pocchitsu (discord)

// Modified and/or redistributed under SS220 CLA with hosting restrictions.
// Full license text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Ninja.Objectives.LeaveNoTrace.Events;

[ByRefEvent]
public record struct NinjaHideAttemptEvent(bool Cancelled = false);
