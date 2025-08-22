// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Content.Shared.Item;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.SwitchableWeapon;

[RegisterComponent]
public sealed partial class SwitchableWeaponComponent : Component
{
    [DataField]
    public DamageSpecifier DamageFolded = new()
    {
        DamageDict = new()
        {
            { "Blunt", 0.0f },
        },
    };

    [DataField]
    public DamageSpecifier DamageOpen = new()
    {
        DamageDict = new()
        {
            { "Blunt", 4.0f },
        },
    };

    [DataField]
    public float StaminaDamageFolded;

    [DataField]
    public float StaminaDamageOpen = 28;

    [DataField]
    public bool IsOpen;

    [DataField]
    public SoundSpecifier? OpenSound;

    [DataField]
    public SoundSpecifier? CloseSound;

    [DataField]
    public ProtoId<ItemSizePrototype> SizeOpened = "Large";

    [DataField]
    public ProtoId<ItemSizePrototype> SizeClosed = "Small";
}
