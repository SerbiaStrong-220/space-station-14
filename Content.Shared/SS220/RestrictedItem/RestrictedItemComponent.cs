// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;

namespace Content.Shared.SS220.RestrictedItem;

[RegisterComponent]
public sealed partial class RestrictedItemComponent : Component
{
    /// <summary>
    /// A whitelist for selecting which entity can interact with item
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist? Whitelist;
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
