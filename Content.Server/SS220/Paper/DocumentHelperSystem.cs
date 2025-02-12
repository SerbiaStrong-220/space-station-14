// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Station.Systems;
using Content.Shared.Paper;
using Content.Shared.SS220.Paper;
using Robust.Server.GameObjects;
using System.Linq;
using static Content.Shared.Paper.PaperComponent;

namespace Content.Server.SS220.Paper;

public sealed partial class DocumentHelperSystem : SharedDocumentHelperSystem
{
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();
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
                values = base.GetValuesByOption(option, uid);
                break;
        }

        return values;
    }

    public override void UpdateUserInterface(Entity<PaperComponent> entity, EntityUid actor)
    {
        base.UpdateUserInterface(entity, actor);
        var optionValuesPair = GetOptionValuesPair(DocumentHelperOptions.All, actor);
        var message = new DocumentHelperOptionsMessage(optionValuesPair);
        _ui.ServerSendUiMessage(entity.Owner, PaperUiKey.Key, message, actor);
    }
    #endregion
}
