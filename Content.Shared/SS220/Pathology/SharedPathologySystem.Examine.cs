// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.HealthExaminable;
using Content.Shared.IdentityManagement;

namespace Content.Shared.SS220.Pathology;

public abstract partial class SharedPathologySystem
{
    private static readonly Color ModerateExamineColor = Color.FromHex("#FFCC44");
    private static readonly Color SevereExamineColor = Color.FromHex("#FF5555");

    private void InitializeExamineEvents()
    {
        SubscribeLocalEvent<PathologyHolderComponent, HealthBeingExaminedEvent>(OnHealthBeingExamined);
    }

    private void OnHealthBeingExamined(Entity<PathologyHolderComponent> entity, ref HealthBeingExaminedEvent args)
    {
        if (entity.Comp.ActivePathologies.Count == 0)
            return;

        var targetIdentity = Identity.Entity(entity.Owner, EntityManager);

        foreach (var (pathologyId, instanceData) in entity.Comp.ActivePathologies)
        {
            if (!_prototype.Resolve(pathologyId, out var pathologyProto))
                continue;

            if (pathologyProto.ExamineDescription is not { } examineLocId)
                continue;

            var color = pathologyProto.IsSevere || instanceData.Level > 0 || instanceData.StackCount > OneStack
                ? SevereExamineColor
                : ModerateExamineColor;

            var text = Loc.GetString(examineLocId, ("target", targetIdentity));

            if (!args.Message.IsEmpty)
                args.Message.PushNewline();

            args.Message.PushColor(color);
            args.Message.AddText(text);
            args.Message.Pop();
        }
    }
}
