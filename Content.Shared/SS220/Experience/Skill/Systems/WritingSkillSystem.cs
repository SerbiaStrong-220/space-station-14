// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Text.RegularExpressions;
using Content.Shared.Paper;
using Content.Shared.Random.Helpers;
using Content.Shared.SS220.Experience.Skill.Components;
using Content.Shared.SS220.Experience.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.Experience.Skill.Systems;

public sealed class WritingSkillSystem : EntitySystem
{
    [Dependency] private readonly ExperienceSystem _experience = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private static readonly string[] TagsForShuffling = { "bold", "italic", "bolditalic" };

    private static readonly Regex TagsForShuffleRegex = new Regex(
        @"\[(bold|italic|bolditalic)\](.*?)\[/\1\]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);

    public override void Initialize()
    {
        base.Initialize();

        _experience.RelayEventToSkillEntity<DisarmChanceChangerSkillComponent, PaperSetContentAttemptEvent>();

        SubscribeLocalEvent<WritingSkillComponent, PaperSetContentAttemptEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<WritingSkillComponent> entity, ref PaperSetContentAttemptEvent args)
    {
        if (entity.Comp.ChangeCaseEach is not null && string.IsNullOrEmpty(args.TransformedContent))
        {
            args.TransformedContent = string.Create(args.TransformedContent.Length, args.TransformedContent, (span, original) =>
            {
                var toUpper = true;
                var counter = 0;
                for (var i = 0; i < span.Length; i++)
                {
                    var oldChar = original[i];

                    span[i] = toUpper ? char.ToUpper(oldChar) : char.ToLower(oldChar);

                    counter++;
                    if (counter >= entity.Comp.ChangeCaseEach)
                    {
                        toUpper = !toUpper;
                        counter = 0;
                    }
                }
            });
        }

        if (entity.Comp.ShuffleMarkupTags)
        {
            var seed = SharedRandomExtensions.HashCodeCombine(new() { (int)_gameTiming.CurTick.Value, GetNetEntity(entity).Id, args.NewContent.Length});
            var rand = new System.Random(seed);

            args.TransformedContent = ShuffleTags(args.TransformedContent, rand);
        }
    }

    private string ShuffleTags(string input, System.Random random)
    {
        if (string.IsNullOrEmpty(input)) return input;

        // MatchEvaluator позволяет генерировать замену динамически для каждого совпадения
        return TagsForShuffleRegex.Replace(input, match =>
        {
            var newTag = random.Pick(TagsForShuffling);
            var content = match.Groups[2].Value;

            return $"[{newTag}]{content}[/{newTag}]";
        });
    }
}
