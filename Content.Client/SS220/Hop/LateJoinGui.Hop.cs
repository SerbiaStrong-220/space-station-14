// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Linq;
using Content.Client.SS220.Hop;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.LateJoin;

public sealed partial class LateJoinGui
{
    private readonly Dictionary<NetEntity, PriorityJobsSection> _prioritySections = new();

    private void AddPrioritySection(NetEntity station, BoxContainer jobList)
    {
        var section = new PriorityJobsSection();
        _prioritySections.Add(station, section);
        jobList.AddChild(section);
        RefreshPrioritySection(station, section);
    }

    private void RefreshPrioritySections()
    {
        foreach (var (station, section) in _prioritySections)
        {
            RefreshPrioritySection(station, section);
        }
    }

    private void RefreshPrioritySection(NetEntity station, PriorityJobsSection section)
    {
        var jobs = new List<JobPrototype>();
        if (_gameTicker.JobsAvailable.TryGetValue(station, out var available) &&
            _gameTicker.PriorityJobs.TryGetValue(station, out var priorities))
        {
            foreach (var id in priorities)
            {
                if (available.TryGetValue(id, out var slots) && slots != 0 && _prototypeManager.TryIndex(id, out var job))
                    jobs.Add(job);
            }
        }
        jobs.Sort(JobUIComparer.Instance);

        // Leave the controls intact on count-only updates, so a refresh cannot interrupt a click.
        var existing = section.Jobs.Children.Cast<JobButton>().ToArray();
        if (existing.Select(button => button.JobId).SequenceEqual(jobs.Select(job => job.ID)))
            return;

        foreach (var button in existing)
        {
            _jobButtons[station][button.JobId].Remove(button);
        }
        section.Jobs.RemoveAllChildren();
        foreach (var job in jobs)
        {
            section.Jobs.AddChild(CreateJobButton(station, job, _gameTicker.JobsAvailable[station][job.ID]));
        }
        section.Visible = jobs.Count > 0;
    }

    // The highlighted list and department lists share the same job button factory and live slot updates.
    private JobButton CreateJobButton(NetEntity station, JobPrototype job, int? slots)
    {
        var priority = _gameTicker.PriorityJobs.TryGetValue(station, out var priorities) && priorities.Contains(job.ID);
        var row = new LateJoinJobEntry();
        row.JobIcon.Texture = _sprites.Frame0(_prototypeManager.Index(job.Icon).Icon);
        var button = new JobButton(row.JobLabel, job.ID, job.LocalizedName, slots, priority) { Name = job.ID };
        button.AddChild(row);
        button.OnPressed += _ => SelectedId.Invoke((station, button.JobId));

        if (!_jobRequirements.IsAllowed(job, (HumanoidCharacterProfile?) _preferencesManager.Preferences?.SelectedCharacter, out var reason))
        {
            button.Disabled = true;
            if (!reason.IsEmpty)
            {
                var tooltip = new Tooltip();
                tooltip.SetMessage(reason);
                button.TooltipSupplier = _ => tooltip;
            }
            row.LockIcon.Texture = _sprites.Frame0(new SpriteSpecifier.Texture(new("/Textures/Interface/Nano/lock.svg.192dpi.png")));
            row.LockIcon.Visible = true;
        }
        else
        {
            button.Disabled = slots == 0;
        }
        _jobButtons[station].GetOrNew(job.ID).Add(button);
        return button;
    }
}
