// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Hallucination;

public abstract class SharedHallucinationSystem : EntitySystem
{
    /// <summary>
    /// Check if entity is protected from hallucination and if not.
    /// After that checks if hallucination exist and than renews its timer.
    /// Adds component if needed and then after adding hallucination dirties.
    /// Always returns <see langword="false"/> on the Client side
    /// </summary>
    /// <returns> false if protected and true if not</returns>
    public abstract bool TryAdd(EntityUid target, HallucinationSetting hallucination);

    /// <summary>
    /// False if target dont have HallucinationComponent or hallucination doesnt exists otherwise true.
    /// Always returns <see langword="false"/> on the Client side
    /// </summary>
    public abstract bool Remove(Entity<SharedHallucinationComponent> entity, HallucinationSetting hallucination);
}
