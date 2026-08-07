// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Virology;

/// <summary>Data passed to symptom's progress conditions: virus entity, host, symptom state and timing.</summary>
public readonly record struct VirusProgressArgs(
    EntityUid Virus,
    EntityUid Carrier,
    VirusSymptomState Symptom,
    IEntityManager EntityManager,
    TimeSpan CurTime,
    bool IsClient);
