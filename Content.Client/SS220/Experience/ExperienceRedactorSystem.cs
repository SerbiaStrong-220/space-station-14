// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Client.SS220.Experience.Ui;
using Content.Shared.SS220.Experience;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.Experience;

public sealed class ExperienceRedactorSystem : EntitySystem
{
    [Dependency] private readonly ExperienceInfoSystem _experienceInfo = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IUserInterfaceManager _userInterface = default!;

    public Dictionary<int, KnowledgePrototype> CachedIndexedKnowledge { private set; get; } = new();

    private ExperienceRedactorWindow? _window;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypeReload);
        SubscribeNetworkEvent<OpenExperienceRedactorRequest>(OnRedactorRequest);

        ReloadCachedIndexedKnowledge();
    }

    private void OnPrototypeReload(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<KnowledgePrototype>())
            return;

        ReloadCachedIndexedKnowledge();
    }

    private void OnRedactorRequest(OpenExperienceRedactorRequest args)
    {
        _window?.Close();
        _window = _userInterface.CreateWindow<ExperienceRedactorWindow>();
        _window?.OpenCentered();

        if (args.Target is null)
            return;

        _window?.SelectEntity(GetEntity(args.Target.Value));
    }

    public void SendChange(EntityUid target, ExperienceData data)
    {
        RaiseNetworkEvent(new ChangeEntityExperienceAdminRequest(GetNetEntity(target), data));
    }

    private void ReloadCachedIndexedKnowledge()
    {
        CachedIndexedKnowledge = _prototype.EnumeratePrototypes<KnowledgePrototype>()
            .OrderBy(x => Loc.GetString(x.KnowledgeName))
            .Select((x, index) => new { Index = index, Proto = x })
            .ToDictionary(x => x.Index, x => x.Proto);
    }
}
