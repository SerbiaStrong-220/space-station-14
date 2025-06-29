// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Server.SS220.MindSlave.Components;
using Content.Server.SS220.Photocopier;
using Content.Server.SS220.Photocopier.Forms;
using Content.Server.SS220.Text;
using Content.Shared.Dataset;
using Content.Shared.GameTicking;
using Content.Shared.Paper;
using Content.Shared.SS220.Photocopier.Forms.FormManagerShared;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.SS220.MindSlave.Systems;

public sealed partial class MindSlaveStopWordSystem : EntitySystem
{
    [Dependency] private readonly MarkovTextGenerator _markovText = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly PhotocopierSystem _photocopier = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private FormManager? _specificFormManager;

    private const int KeySize = 3;
    private const int StopWordMinSize = 4; // to ignore some common words
    private const string StampComponentName = "Stamp";

    [ValidatePrototypeId<DatasetPrototype>]
    private const string TextDatasetId = "MindSlaveStopWordTexts";

    public string StopWord
    {
        get
        {
            if (_stopWord == string.Empty)
                Log.Error("Asked for mind slave stop word but it is empty!");
            if (!_textGeneratedThisRound)
                Log.Error("Asked for mind slave stop word but it wasnt generated!");
            return _stopWord;
        }
    }

    public string Text
    {
        get
        {
            if (_text == string.Empty)
                Log.Error("Asked for mind slave text but it is empty");
            if (!_textGeneratedThisRound)
                Log.Error("Asked for mind slave text but it wasnt generated!");
            return _text;
        }
    }

    private string _stopWord = string.Empty;
    private string _text = string.Empty;
    private bool _textGeneratedThisRound = false;

    public override void Initialize()
    {
        base.Initialize();
        _specificFormManager = EntityManager.System<FormManager>();

        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStart);
        SubscribeLocalEvent<RoundEndedEvent>(OnRoundEnded);

        SubscribeLocalEvent<MindSlaveStopWordContainerComponent, MapInitEvent>(OnInit);
    }

    private void OnRoundStart(RoundStartedEvent _)
    {
        if (!_textGeneratedThisRound)
            MakeTextAndStopWord();
    }

    private void MakeTextAndStopWord()
    {
        _markovText.Initialize(_prototype.Index<DatasetPrototype>(TextDatasetId).Values, KeySize);
        _text = _markovText.GenerateText(83);
        _textGeneratedThisRound = true;
        _stopWord = _markovText.ReplacePunctuationInEnding(_random.Pick(_text.Split().Where(x => x.Length >= StopWordMinSize).ToArray()));
        RaiseLocalEvent(new StopWordGeneratedEvent(_stopWord));
    }

    private void OnRoundEnded(RoundEndedEvent args)
    {
        _markovText.CleatData();
        _stopWord = string.Empty;
        _text = string.Empty;
        _textGeneratedThisRound = false;
    }

    private void OnInit(Entity<MindSlaveStopWordContainerComponent> entity, ref MapInitEvent args)
    {
        if (!_textGeneratedThisRound)
            MakeTextAndStopWord();

        if (_specificFormManager is null)
            return;

        var form = _specificFormManager.TryGetFormFromDescriptor(new FormDescriptor(entity.Comp.Collection, entity.Comp.Group, entity.Comp.Form));
        if (form == null)
        {
            Log.Error($"Invalid form description for entity{ToPrettyString(entity)}");
            return;
        }

        _photocopier.FormToDataToCopy(form, out var dataToCopy, out var metaData);
        var spawnedForm = _photocopier.SpawnCopy(Transform(entity).Coordinates, metaData, dataToCopy);

        if (!TryComp<PaperComponent>(spawnedForm, out var paperComponent)
            || !TryComp<PaperComponent>(entity, out var entityPaperComponent))
            return;
        // idea was that hos documents is container of this information
        _paper.SetContent((entity.Owner, entityPaperComponent), paperComponent.Content.Replace("mindslave-stop-word-text", Text));
        QueueDel(spawnedForm);

        foreach (var stampProtoId in entity.Comp.StampList)
        {
            var stampProto = _prototype.Index(stampProtoId);
            if (!stampProto.Components.TryGetComponent(StampComponentName, out var component)
                || component is not StampComponent stampComponent)
            {
                Log.Error("Passed entity prototype id in mind slave stop word container's stamp list, but entry dont have stampComponent");
                return;
            }


            _paper.TryStamp((entity.Owner, entityPaperComponent), new StampDisplayInfo()
            {
                StampedColor = stampComponent.StampedColor,
                StampedName = stampComponent.StampedName
            },
            stampComponent.StampState);
        }
    }
}
