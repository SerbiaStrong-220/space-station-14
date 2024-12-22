// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Linq;
using Content.Client.GameTicking.Managers;
using Content.Client.RoundEnd;
using Content.Client.SS220.RoundEnd.UI;
using Content.Shared.GameTicking;
using Content.Shared.Input;
using Content.Shared.Roles;
using Content.Shared.SS220.CCVars;
using Content.Shared.SS220.Discord;
using Content.Shared.SS220.Utility;
using JetBrains.Annotations;
using Robust.Client.Input;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Configuration;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;

namespace Content.Client.SS220.RoundEnd;

/// <remarks>
/// SS220 re-implementation of <see cref="Content.Client.RoundEnd.RoundEndSummaryUIController"/>,
/// you may refer to it if something breaks here.
/// </remarks>
[UsedImplicitly]
public sealed class RoundEndSummaryUIController : UIController,
    IOnSystemLoaded<ClientGameTicker>
{
    [Dependency] private readonly IInputManager _input = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;

    private const bool IS_DEBUG_MODE = false; // ALWAYS turn this off when pushing to repo

    private RoundEndSummaryWindow? _window;
    private RoundEndTitlesWindow? _titles;
    private RoundEndMessageEvent? _message; // DEBUG 

    public void OnSystemLoaded(ClientGameTicker system)
    {
        _input.SetInputCommand(ContentKeyFunctions.ToggleRoundEndSummaryWindow,
            InputCmdHandler.FromDelegate(ToggleScoreboardWindow, ToggleScoreboardWindow));
    }

    public void OpenRoundEndSummaryWindow(RoundEndMessageEvent message)
    {
        // Don't open duplicate windows (mainly for replays).
        if (_window?.RoundId == message.RoundId)
            return;

        _window = new RoundEndSummaryWindow(message.GamemodeTitle, message.RoundEndText,
            message.RoundDuration, message.RoundId, message.AllPlayersEndInfo, EntityManager);
        if (_configurationManager.GetCVar(CCVars220.RoundEndTitlesOpenMode) != RoundEndTitlesMode.DoNotOpen)
        {
            CreateTitles(message);
            OpenTitlesWindow();
        }
    }

    private void ToggleScoreboardWindow(ICommonSession? session = null)
    {
        var isOpen = (_window?.IsOpen ?? false) || (_titles?.IsOpen ?? false);
        if (isOpen)
        {
            _window?.Close();
            _titles?.Close();
        }
        else
        {
            _window?.OpenCenteredRight();
            _window?.MoveToFront();
            DebugRecreateTitles();
            OpenTitlesWindow();
        }
    }

    private void OpenTitlesWindow()
    {
        if (_titles is null)
            return;
        _titles.Open();
        _titles.MoveToFront();
        var openMode = _configurationManager.GetCVar(CCVars220.RoundEndTitlesOpenMode);
        if (openMode == RoundEndTitlesMode.Fullscreen)
        {
            _titles.Maximaze();
        }
        else
        {
            _titles.Minimize();
        }
    }

    private void CreateTitles(RoundEndMessageEvent? message)
    {
        if (message is null)
            return;
        var players = message.AllPlayersEndInfo;
        var sponsors = message.Sponsors;

#if DEBUG
        if (IS_DEBUG_MODE)
        {
            players = players.Concat(Faker.FakeArray(50, () => new RoundEndMessageEvent.RoundEndPlayerInfo
            {
                PlayerOOCName = Faker.FakeUsername(),
                PlayerICName = Faker.FakeCharacterName(),
                PlayerNetEntity = players.ElementAtOrDefault(0).PlayerNetEntity,
                JobPrototypes = Faker.FakeArray(1, () => Faker.FakeProtoId<JobPrototype>()),
                AntagPrototypes = Faker.FakeArray(0, 1, () => Faker.FakeProtoId<AntagPrototype>()),
                Role = "Dummy Role",
            })).ToArray();
            sponsors = sponsors.Concat(Faker.FakeArray(50, () => new RoundEndMessageEvent.RoundEndSponsorInfo
            {
                PlayerOOCName = Faker.FakeUsername(),
                Tiers = Faker.FakeArray(1, () => Faker.From(new[]
                {
                    SponsorTier.CriticalMassShlopa,
                    SponsorTier.GoldenShlopa,
                    SponsorTier.HugeShlopa,
                })),
            })).ToArray();
            _message = message;
        }
#endif

        _titles = new RoundEndTitlesWindow(message.GamemodeTitle, message.RoundEndText,
            message.RoundDuration, message.RoundId, players, sponsors);
    }

    private void DebugRecreateTitles()
    {
#if DEBUG
        if (IS_DEBUG_MODE)
        {
            CreateTitles(_message);
        }
#endif
    }
}
