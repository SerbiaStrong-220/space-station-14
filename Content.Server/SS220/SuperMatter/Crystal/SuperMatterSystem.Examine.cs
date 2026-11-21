// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Globalization;
using Content.Server.SS220.SuperMatter.Crystal.Components;
using Content.Shared.Examine;
using Content.Shared.Ghost;

namespace Content.Server.SS220.SuperMatter.Crystal;

public sealed partial class SuperMatterSystem
{
    private void InitializeExamine()
    {
        SubscribeLocalEvent<SuperMatterComponent, ExaminedEvent>(OnCrystalExamined);
    }

    private void OnCrystalExamined(Entity<SuperMatterComponent> ent, ref ExaminedEvent args)
    {
        if (!HasComp<GhostComponent>(args.Examiner))
            return;

        var integrity = GetIntegrity(ent.Comp).ToString("F1", CultureInfo.InvariantCulture);
        args.PushMarkup(Loc.GetString("supermatter-examine-integrity", ("integrity", integrity)));
    }
}
