// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Examine;
using Content.Shared.Mindshield.Components;
using Content.Shared.SS220.CultYogg.CultYoggIcons;

namespace Content.Shared.SS220.CultYogg.Unenslavable;

public sealed class UnenslavableSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UnenslavableComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<UnenslavableComponent> ent, ref ExaminedEvent args)
    {
        if (!HasComp<ShowCultYoggIconsComponent>(args.Examiner))
            return;

        if (HasComp<MindShieldComponent>(ent))
            args.PushMarkup(Loc.GetString("cult-yogg-unenslavable-mindshield"));
    }
}
