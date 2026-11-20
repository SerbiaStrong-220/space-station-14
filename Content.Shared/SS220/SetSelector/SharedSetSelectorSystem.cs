// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.SetSelector;

public abstract class SharedSetSelectorSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SetSelectorComponent, SetSelectorChangeSetMessage>(OnChangeSet);
    }

    private void OnChangeSet(Entity<SetSelectorComponent> selector, ref SetSelectorChangeSetMessage args)
    {
        if (args.SetNumber < 0 || args.SetNumber >= selector.Comp.PossibleSets.Count)
            return;

        var selected = selector.Comp.SelectedSets;
        if (!selected.Remove(args.SetNumber))
        {
            if (selected.Count >= selector.Comp.MaxSelectedSets)
            {
                if (selector.Comp.MaxSelectedSets != 1)
                    return;

                selected.Clear();
            }

            selected.Add(args.SetNumber);
        }

        Dirty(selector);
        UpdateUI(selector);
    }

    protected virtual void UpdateUI(Entity<SetSelectorComponent> selector)
    {
    }
}
