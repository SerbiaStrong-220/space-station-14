// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Popups;
using Content.Shared.SS220.Experience.Systems;
using Content.Shared.UserInterface;

namespace Content.Shared.SS220.Experience.SkillChecks.Components;

public sealed class ActivatableUIRequiresSkillSystem : EntitySystem
{
    [Dependency] private readonly ExperienceSystem _experience = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActivatableUIRequiresSkillComponent, ActivatableUIOpenAttemptEvent>(OnUIOpenAttempt);
    }

    private void OnUIOpenAttempt(Entity<ActivatableUIRequiresSkillComponent> activatableUI, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!_experience.TryGetAcquiredSkills(args.User, activatableUI.Comp.TreeProtoId).Contains(activatableUI.Comp.SkillProtoId))
        {
            args.Cancel();
            if (activatableUI.Comp.PopupMessage != null)
                _popup.PopupClient(Loc.GetString(activatableUI.Comp.PopupMessage), activatableUI, args.User);
        }
    }
}
