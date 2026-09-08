// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions.Events;
using Content.Shared.Popups;
using Content.Shared.SS220.Experience.Skill.Components;

namespace Content.Shared.SS220.Experience.Skill.Systems;

public sealed partial class PassiveBlockModifierSystem : SkillEntitySystem
{

    public override void Initialize()
    {
        base.Initialize();

        SubscribeEventToSkillEntity<PassiveBlockModifierComponent, ModifyPassiveBlock>(OnModifyPassiveBlock);
    }

    private void OnModifyPassiveBlock(Entity<PassiveBlockModifierComponent> entity, ref ModifyPassiveBlock args)
    {
        args.MeleeBlockChance *= entity.Comp.PassiveMeleeBlockModifier;
        args.RangeBlockChance *= entity.Comp.PassiveRangeBlockModifier;
    }
}

[ByRefEvent]
public record struct ModifyPassiveBlock(float MeleeBlockChance, float RangeBlockChance);
