// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Station.Systems;
using Content.Shared.Paper;
using Content.Shared.SS220.Paper;
using Robust.Server.GameObjects;
using System.Linq;

namespace Content.Server.SS220.Paper;

public sealed partial class DocumentHelperSystem : SharedDocumentHelperSystem
{
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaperComponent, DocumentHelperRequestInfoBuiMessage>(OnUIRequestInfo);
    }

    #region Ui
    public override List<string> GetValuesByOption(DocumentHelperOptions option, EntityUid? uid = null)
    {
        List<string> values = [];
        switch (option)
        {
            case DocumentHelperOptions.Station:
                values = values.Union(_stationSystem.GetStationNames().Select(x => x.Name)).ToList();
                break;
            default:
                base.GetValuesByOption(option, uid);
                break;
        }

        return values;
    }

    private void OnUIRequestInfo(Entity<PaperComponent> entity, ref DocumentHelperRequestInfoBuiMessage args)
    {
        var optionValuesPair = GetOptionValuesPair(args.Options, args.Actor);
        var state = new DocumentHelperBuiState(optionValuesPair);
        _ui.SetUiState(entity.Owner, args.UiKey, state);
    }
    #endregion
}
