// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Text.RegularExpressions;
using Content.Shared.Paper;
using Content.Shared.SS220.Experience.Skill.Components;
using Content.Shared.SS220.Experience.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.Experience.Skill.Systems;

public sealed class WritingSkillIssueSystem : SkillEntitySystem
{
    [Dependency] private readonly ExperienceSystem _experience = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private static readonly string[] TagsForShuffling = { "bold", "italic", "bolditalic", "head=1", "head=2", "head=3" };

    private static readonly Regex TagsForShuffleRegex = new Regex(
        @"\[(bold|italic|bolditalic|head)(?:=\d+)?\](.*?)\[/\1\]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeEventToSkillEntity<WritingSkillIssueComponent, PaperSetContentAttemptEvent>(OnPaperSetContentAttemptEvent);
    }

    private void OnPaperSetContentAttemptEvent(Entity<WritingSkillIssueComponent> entity, ref PaperSetContentAttemptEvent args)
    {
        if (entity.Comp.ShuffleMarkupTags)
        {
            args.TransformedContent = ShuffleTags(args.Paper.Comp.Content, args.TransformedContent, GetPredictedRandomOnCurTick(new() { GetNetEntity(entity).Id, args.NewContent.Length }));
        }
    }

    private string ShuffleTags(string oldText, string newInput, System.Random random)
    {
        if (string.IsNullOrEmpty(newInput))
            return newInput;

        var oldBlocksCounts = new Dictionary<string, int>();
        if (!string.IsNullOrEmpty(oldText))
        {
            foreach (Match match in TagsForShuffleRegex.Matches(oldText))
            {
                if (!oldBlocksCounts.TryAdd(match.Value, 1))
                {
                    oldBlocksCounts[match.Value]++;
                }
            }
        }

        return TagsForShuffleRegex.Replace(newInput, match =>
        {
            var currentBlock = match.Value;

            if (oldBlocksCounts.TryGetValue(currentBlock, out int count) && count > 0)
            {
                oldBlocksCounts[currentBlock] = count - 1;
                return currentBlock;
            }

            var newTag = TagsForShuffling[random.Next(TagsForShuffling.Length)];
            // to correctly close head tag
            var closureTag = newTag.Split('=')[0];

            var content = match.Groups[2].Value;

            return $"[{newTag}]{content}[/{closureTag}]";
        });
    }
}
