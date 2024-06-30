using Robust.Shared.Prototypes;

namespace Content.Server.Corvax.StationGoal
{
    [Serializable, Prototype("stationGoal")]
    public sealed class StationGoalPrototype : IPrototype
    {
        [IdDataFieldAttribute] public string ID { get; } = default!;

        [DataField("text")] public string Text { get; set; } = string.Empty;

        [DataField("goalType")] public GoalType GoalType { get; set; } = GoalType.AnyPopulation;
    }

    /// <summary>
    /// Type of goal to divide into goals that can be completed with a large or small number of players.
    /// </summary>
    public enum GoalType
    {
        AnyPopulation,
        HighPopulation,
        LowPopulation
    }
}
