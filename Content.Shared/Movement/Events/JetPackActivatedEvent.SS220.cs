// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Movement.Components;

namespace Content.Shared.Movement.Events;

/// <summary>
/// Raised on an user whenever it activates jet pack
/// </summary>
[ByRefEvent]
public record struct JetPackActivatedEvent(Entity<ActiveJetpackComponent?> JetPack);
