using Content.Shared.Damage;

namespace Content.Server.SS220.AntagItem
{
    [RegisterComponent, AutoGenerateComponentState]
    public sealed partial class AntagItemComponent : Component
    {
        [DataField("dropDamage"), ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier Damage = new DamageSpecifier();
        [DataField("dropText"), ViewVariables(VVAccess.ReadWrite)]
        public string DropText = string.Empty;


    }
}
