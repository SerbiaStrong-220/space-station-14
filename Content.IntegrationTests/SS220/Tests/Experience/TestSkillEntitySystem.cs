// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Experience.Skill;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.IntegrationTests.SS220.Tests.Experience;


public sealed class TestSkillEntitySystem : SkillEntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeEventToSkillEntity<TestSkillEntityComponent, TestSkillEntityEvent>(OnTestSkillEntityEvent);
    }

    private void OnTestSkillEntityEvent(Entity<TestSkillEntityComponent> entity, ref TestSkillEntityEvent _)
    {
        entity.Comp.ReceivedEvent = true;
    }
}

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TestSkillEntityComponent : Component
{
    [AutoNetworkedField]
    public bool ReceivedEvent = false;
}

[ByRefEvent]
public record struct TestSkillEntityEvent();
