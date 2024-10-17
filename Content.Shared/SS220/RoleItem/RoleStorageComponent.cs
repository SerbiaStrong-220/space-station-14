// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Roles;

namespace Content.Shared.SS220.RoleItem;

[RegisterComponent]
public sealed partial class RoleStorageComponent : Component
{
    /// <summary>
    /// Takes <seealso cref="AntagPrototype"/> ID.
    /// </summary>
    [DataField(required: true)]
    public string RoleId;
}
