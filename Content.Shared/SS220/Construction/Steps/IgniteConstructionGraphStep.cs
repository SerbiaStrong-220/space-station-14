// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Construction;
using Content.Shared.Construction.Steps;
using Content.Shared.Examine;

namespace Content.Shared.SS220.Construction.Steps
{
    [DataDefinition]
    public sealed partial class IgniteConstructionGraphStep : ConstructionGraphStep
    {
        [DataField("ignite")]
        public IgniteConstructionGraphStepData Ignite { get; private set; } = new();

        public override void DoExamine(ExaminedEvent examinedEvent)
        {
            examinedEvent.PushMarkup(Loc.GetString("construction-use-igniting-entity"));
        }

        public override ConstructionGuideEntry GenerateGuideEntry()
        {
            return new ConstructionGuideEntry
            {
                Localization = "construction-presenter-ignite-step",
            };
        }
    }

    [DataDefinition]
    public sealed partial class IgniteConstructionGraphStepData
    {
    }
}
