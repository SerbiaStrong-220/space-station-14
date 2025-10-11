using System.Linq;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Pipes.DisposalFilter;

public interface IFilterCondition
{
    bool Matches(EntityUid ent, IEntityManager entMan);

    IEnumerable<(string Text, Action Remove)> GetSubItems(Action<IFilterCondition> removeAction);
}

[Serializable, NetSerializable]
public sealed class NameContainsFilter : IFilterCondition
{
    [DataField] public List<string> ContainNames = new();

    public bool Matches(EntityUid ent, IEntityManager entMan)
    {
        if (!entMan.TryGetComponent(ent, out MetaDataComponent? meta))
            return false;

        foreach (var name in ContainNames)
        {
            if (meta.EntityName.Contains(name, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public IEnumerable<(string Text, Action Remove)> GetSubItems(Action<IFilterCondition> removeAction)
    {
        foreach (var name in ContainNames)
        {
            var locName = Loc.GetString("filter-name", ("name", name));
            yield return (locName, () =>
            {
                ContainNames.Remove(name);
                if (ContainNames.Count == 0)
                    removeAction(this);
            });
        }
    }

    public void AddEntry(object entry)
    {
        if (entry is List<string> list)
        {
            ContainNames = list;
        }
    }
}

[Serializable, NetSerializable]
public sealed class WhitelistFilter : IFilterCondition
{
    [DataField] public EntityWhitelist Whitelist = new();

    public bool Matches(EntityUid ent, IEntityManager entMan)
    {
        return entMan.System<EntityWhitelistSystem>().IsWhitelistPass(Whitelist, ent);
    }

    public IEnumerable<(string Text, Action Remove)> GetSubItems(Action<IFilterCondition> removeAction)
    {
        if (Whitelist.Components?.Length > 0)
        {
            foreach (var comp in Whitelist.Components)
            {
                var locComp = Loc.GetString($"filter-{comp}");
                var loc = Loc.GetString("filter-whitelist", ("name", locComp));

                yield return (loc, () =>
                {
                    Whitelist.Components = Whitelist.Components
                        .Where(c => c != comp)
                        .ToArray();

                    if (IsEmpty())
                        removeAction(this);
                });
            }
        }

        if (Whitelist.Tags?.Count > 0)
        {
            foreach (var tag in Whitelist.Tags.ToArray())
            {
                var locTag = Loc.GetString($"filter-{tag}");
                var loc = Loc.GetString("filter-whitelist", ("name", locTag));
                yield return (loc, () =>
                {
                    Whitelist.Tags.Remove(tag);
                    if (IsEmpty())
                        removeAction(this);
                });
            }
        }
    }

    public void AddEntry(object entry)
    {
        switch (entry)
        {
            case string comp:
                Whitelist.Components ??= [];
                Whitelist.Components = !Whitelist.Components.Contains(comp)
                    ? Whitelist.Components.Append(comp).ToArray()
                    : Whitelist.Components.Where(c => c != comp).ToArray();

                return;
            case ProtoId<TagPrototype> tag:
                Whitelist.Tags ??= [];
                Whitelist.Tags = !Whitelist.Tags.Contains(tag)
                    ? Whitelist.Tags.Append(tag).ToList()
                    : Whitelist.Tags.Where(t => t != tag).ToList();

                return;
        }
    }

    private bool IsEmpty()
    {
        return (Whitelist.Components == null || Whitelist.Components.Length == 0)
               && (Whitelist.Tags == null || Whitelist.Tags.Count == 0)
               && (Whitelist.Sizes == null || Whitelist.Sizes.Count == 0);
    }
}

[Serializable, NetSerializable]
public sealed class FilterRule
{
    [DataField] public List<IFilterCondition> Conditions = new();
    [DataField] public bool RequiredAll;
    [DataField] public Direction OutputDir = Direction.Invalid;

    public bool Matches(EntityUid ent, IEntityManager entMan)
    {
        if (Conditions.Count == 0)
            return false;

        var results = Conditions.Select(cond => cond.Matches(ent, entMan)).ToList();

        return RequiredAll
            ? results.All(r => r)
            : results.Any(r => r);
    }

    public T EnsureFilter<T>() where T : IFilterCondition, new()
    {
        var filter = Conditions.OfType<T>().FirstOrDefault();
        if (filter == null)
        {
            filter = IoCManager.Resolve<IDynamicTypeFactory>().CreateInstance<T>();
            Conditions.Add(filter);
        }

        return filter;
    }

    public void Clear()
    {
        Conditions.Clear();
    }
}

[Serializable, NetSerializable]
public enum DisposalFilterUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class DisposalFilterBoundState(List<FilterRule> dirByRules, Direction baseDir)
    : BoundUserInterfaceState
{
    public List<FilterRule> DirByRules = dirByRules;
    public Direction BaseDir = baseDir;
}

[Serializable, NetSerializable]
public sealed class DisposalFilterBoundMessage(List<FilterRule> dirByRules, Direction baseDir)
    : BoundUserInterfaceMessage
{
    public List<FilterRule> DirByRules = dirByRules;
    public Direction BaseDir = baseDir;
}
