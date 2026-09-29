// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
namespace Content.Shared.SS220.MalfAI;

[RegisterComponent]
public sealed partial class MalfAiUpgradeableTurretComponent : Component
{
    [DataField]
    public float FireRateMult = 2f;

    [DataField]
    public float DamageMult = 1.25f;

    [DataField]
    public int HpBonus = 30;
}
