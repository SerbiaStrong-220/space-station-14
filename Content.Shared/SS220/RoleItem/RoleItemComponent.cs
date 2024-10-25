// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Robust.Shared.Audio;

namespace Content.Shared.SS220.RoleItem;

[RegisterComponent]
public sealed partial class RoleItemComponent : Component
{
    /// <summary>
    /// Takes <see cref="AntagPrototype"/> ID.
    /// </summary>
    [DataField(required: true)]
    public string RoleId;
    /// <summary>
    /// Popup occurs when the player does not have a role
    /// </summary>
    [DataField]
    public string LocalizedPopup = "default-role-item-popup";
    [DataField]
    public SoundSpecifier? SoundOnFail;
    [DataField]
    public DamageSpecifier DamageOnFail = new DamageSpecifier();

}
