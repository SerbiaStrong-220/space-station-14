// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Collections.Generic;
using Content.Shared.SS220.Pathology;

namespace Content.Server.SS220.Pathology;

public sealed partial class PathologySystem
{
    public List<string> GetAnalyzerVirusLines(Entity<PathologyHolderComponent?> entity)
    {
        var lines = new List<string>();

        if (!Resolve(entity.Owner, ref entity.Comp, false))
            return lines;

        if (entity.Comp.ActiveViruses.Count > 0)
            lines.Add(Loc.GetString("health-analyzer-report-disease-detected"));

        return lines;
    }
}
