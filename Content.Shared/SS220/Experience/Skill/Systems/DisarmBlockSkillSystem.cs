// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions.Events;
using Content.Shared.Popups;
using Content.Shared.SS220.Experience.Skill.Components;

namespace Content.Shared.SS220.Experience.Skill.Systems;

public sealed class DisarmBlockSkillSystem : SkillEntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeEventToSkillEntity<DisarmBlockSkillComponent, DisarmAttemptEvent>(OnDisarmAttempt);
    }

    private void OnDisarmAttempt(Entity<DisarmBlockSkillComponent> entity, ref DisarmAttemptEvent args)
    {
        args.Cancelled = true;

        if (args.TargetItemInHandUid is {} item)
            _popupSystem.PopupEntity(Loc.GetString(entity.Comp.DisarmBlockedPopupItem, ("target", args.TargetUid), ("item", item)), entity, PopupType.SmallCaution);
        else
            _popupSystem.PopupEntity(Loc.GetString(entity.Comp.DisarmBlockedPopupHand, ("target", args.TargetUid), ("performer", args.DisarmerUid)), entity, PopupType.SmallCaution);
    }
}
