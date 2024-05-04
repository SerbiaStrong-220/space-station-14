namespace Content.Server.SS220.Atmos
{
    /// <summary>
    ///     Self-refilling tank.
    /// </summary>
    [RegisterComponent]
    public sealed partial class GasTankSelfRefillComponent : Component
    {
        [ViewVariables(VVAccess.ReadWrite)] [DataField("autoRefillRate")] public float AutoRefillRate { get; set; } = 0.1f; //slow gain over frame.
    }
}
