using Content.Server.Body.Systems;
using Content.Server.SS220.Surgery.Components;
using Content.Server.SS220.Surgery.Components.Instruments;
using Content.Shared.Body.Components;
using Content.Shared.Body.Organ;
using Content.Shared.Body.Part;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.SS220.Surgery;
using Content.Shared.SS220.Surgery.Systems;
using Content.Shared.Verbs;

namespace Content.Server.SS220.Surgery.Systems;

public sealed partial class SurgicalOrganManipulationSystem : EntitySystem
{
    [Dependency] private readonly BodySystem _bodySystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OrganComponent, AfterInteractEvent>(PullInAfterInteract);
        SubscribeLocalEvent<BodyComponent, PullOutOrganDoAfterEvent>(OnPullOutDoAfter);

    }


    public void ToggleOrganManipulationMode(EntityUid target, OperapableComponent comp, GetVerbsEvent<Verb> args)
    {
        if (!TryComp<BodyComponent>(target, out var bodyComp) || !args.CanAccess)
            return;

        Verb head = new()
        {
            Text = "Голова",
            Act = () =>
            {
                comp.CurrentOperatedBodyPart = BodyPartType.Head;
            },
            Category = VerbCategory.BodyPartList
        };

        args.Verbs.Add(head);

        Verb body = new()
        {
            Text = "Туловище",
            Act = () =>
            {
                comp.CurrentOperatedBodyPart = BodyPartType.Torso;
            },
            Category = VerbCategory.BodyPartList
        };
        args.Verbs.Add(body);

    }

    public void OnPullOutDoAfter(EntityUid uid, BodyComponent component, DoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;
        if (!TryComp<SurgicalClampComponent>(args.Used, out var clamp))
            return;

        _bodySystem.DropOrgan(clamp.SelectedOrgan);
    }

    public void PullInAfterInteract(EntityUid uid, OrganComponent component, AfterInteractEvent args)
    {
        if (args.Target is null || !args.CanReach)
            return;
        if (!TryComp<HandsComponent>(args.User, out var hands))
            return;
        if (!TryComp<OrganComponent>(hands.ActiveHandEntity, out var organ))
            return;
        // _bodySystem.InsertOrgan(hands.ActiveHandEntity);
    }
    public bool TryPullOutOrgan()
    {
        return false;
    }

}
