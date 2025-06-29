// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Cloning;
using Content.Shared.Cloning.Events;
using Robust.Shared.Serialization.Manager;

namespace Content.Server.SS220.DefibrillatorSkill;

public sealed partial class DefibrillatorSkillSystem : EntitySystem
{
    [Dependency] private readonly ISerializationManager _serialization = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DefibrillatorSkillComponent, CloningEvent>(OnCloning);
    }

    private void OnCloning(Entity<DefibrillatorSkillComponent> entity, ref CloningEvent args)
    {
        var targetComp = EnsureComp<DefibrillatorSkillComponent>(args.CloneUid);
        _serialization.CopyTo(entity.Comp, ref targetComp, notNullableOverride: true);
    }
}
