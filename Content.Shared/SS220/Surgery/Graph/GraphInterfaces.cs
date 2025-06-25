// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.SS220.Surgery.Graph;

/// <summary>
/// Used to define if this person can make surgery.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public partial interface IAbstractSurgeryGraphAvailabilityCondition
{
    /// <summary>
    /// Checks if specified condition is passed
    /// </summary>
    /// <param name="uid"> Condition target </param>
    /// <param name="reason"> Could be null if condition result is true </param>
    /// <returns></returns>
    bool Condition(EntityUid uid, IEntityManager entityManager, [NotNullWhen(false)] out string? reason);
}

[Serializable]
[DataDefinition]
public partial struct FlippingCondition<T>
{
    [DataField(required: true)]
    public T Value;

    [DataField(required: true)]
    public FlippingConditionType ConditionType;

    /// <summary>
    /// Checks if passed under ConditionType.
    /// </summary>
    /// <param name="forwardCheck">True if passed forward arm</param>
    /// <param name="reverseCheck">True if passed reverse arm</param>
    public bool IsPassed(Func<T, bool> forwardCheck, Func<T, bool> reverseCheck)
    {
        switch (ConditionType)
        {
            case FlippingConditionType.Straight:
                if (forwardCheck(Value))
                    return true;
                break;
            case FlippingConditionType.Reverse:
                if (reverseCheck(Value))
                    return true;
                break;
        }
        return false;
    }
}

public enum FlippingConditionType
{
    Straight,
    Reverse
}
