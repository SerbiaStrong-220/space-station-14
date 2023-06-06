namespace Content.Server.SS220.CryopodSSD;


/// <summary>
/// Entity that have this component also should have
/// Climbable component
/// </summary>
[RegisterComponent]
public sealed class CryopodSSDComponent : Component
{
    /// <summary>
    /// List for IC knowing who went in cryo
    /// </summary>
    [DataField("LoggedEntities")]
    public List<string> StoredEntities = new List<string>();
}