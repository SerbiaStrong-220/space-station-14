using System.Linq;
using System.Numerics;
using Content.Client.Message;
using Content.Client.SS220.RoundEndInfo;
using Content.Shared.GameTicking;
using Content.Shared.SS220.RoundEndInfo;
using Content.Shared.Store;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client.RoundEnd
{
    public sealed class RoundEndSummaryWindow : DefaultWindow
    {
        //ss220 add additional info for round start
        [Dependency] private readonly IPrototypeManager _proto = default!;
        //ss220 add additional info for round end

        private readonly IEntityManager _entityManager;
        private readonly RoundEndInfoSystem _roundEndInfo;

        public int RoundId;

        //ss220 add additional info for round start
        private BoxContainer? _summaryContentContainer;
        //ss220 add additional info for round end

        public RoundEndSummaryWindow(string gm, string roundEnd, TimeSpan roundTimeSpan, int roundId,
            RoundEndMessageEvent.RoundEndPlayerInfo[] info, IEntityManager entityManager)
        {
            //ss220 add additional info for round start
            IoCManager.InjectDependencies(this);

            _roundEndInfo = entityManager.System<RoundEndInfoSystem>();
            //ss220 add additional info for round end

            _entityManager = entityManager;

            MinSize = SetSize = new Vector2(520, 580);

            Title = Loc.GetString("round-end-summary-window-title");

            // The round end window is split into two tabs, one about the round stats
            // and the other is a list of RoundEndPlayerInfo for each player.
            // This tab would be a good place for things like: "x many people died.",
            // "clown slipped the crew x times.", "x shots were fired this round.", etc.
            // Also good for serious info.

            RoundId = roundId;
            var roundEndTabs = new TabContainer();
            roundEndTabs.AddChild(MakeRoundEndSummaryTab(gm, roundEnd, roundTimeSpan, roundId));
            roundEndTabs.AddChild(MakePlayerManifestTab(info));

            Contents.AddChild(roundEndTabs);

            OpenCenteredRight();
            MoveToFront();
        }

        private BoxContainer MakeRoundEndSummaryTab(string gamemode, string roundEnd, TimeSpan roundDuration, int roundId)
        {
            var roundEndSummaryTab = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Name = Loc.GetString("round-end-summary-window-round-end-summary-tab-title")
            };

            var roundEndSummaryContainerScrollbox = new ScrollContainer
            {
                VerticalExpand = true,
                Margin = new Thickness(10)
            };
            var roundEndSummaryContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical
            };

            //Gamemode Name
            var gamemodeLabel = new RichTextLabel();
            var gamemodeMessage = new FormattedMessage();
            gamemodeMessage.AddMarkupOrThrow(Loc.GetString("round-end-summary-window-round-id-label", ("roundId", roundId)));
            gamemodeMessage.AddText(" ");
            // SS220 Round End Titles begin
            //gamemodeMessage.AddMarkupOrThrow(Loc.GetString("round-end-summary-window-gamemode-name-label", ("gamemode", gamemode)));
            gamemodeMessage.AddMarkupOrThrow(Loc.GetString("round-end-summary-window-gamemode-name-label", ("gamemode", Loc.GetString(gamemode))));
            // SS220 Round End Titles end
            gamemodeLabel.SetMessage(gamemodeMessage);
            roundEndSummaryContainer.AddChild(gamemodeLabel);

            //Duration
            var roundTimeLabel = new RichTextLabel();
            roundTimeLabel.SetMarkup(Loc.GetString("round-end-summary-window-duration-label",
                                                   ("hours", roundDuration.Hours),
                                                   ("minutes", roundDuration.Minutes),
                                                   ("seconds", roundDuration.Seconds)));
            roundEndSummaryContainer.AddChild(roundTimeLabel);

            //Round end text
            if (!string.IsNullOrEmpty(roundEnd))
            {
                var roundEndLabel = new RichTextLabel();
                roundEndLabel.SetMarkup(roundEnd);
                roundEndSummaryContainer.AddChild(roundEndLabel);
            }

            roundEndSummaryContainerScrollbox.AddChild(roundEndSummaryContainer);
            roundEndSummaryTab.AddChild(roundEndSummaryContainerScrollbox);

            //ss220 add additional info for round start
            roundEndSummaryContainer.AddChild(MakeAntagItemsSection());

            _summaryContentContainer = roundEndSummaryContainer;
            //ss220 add additional info for round end
            return roundEndSummaryTab;
        }

        //ss220 add additional info for round start
        /// <summary>
        /// Provides UI construction and layout logic for additional round-end summary information,
        /// including custom stat blocks and antagonist purchase data.
        /// </summary>
        public void PopulateAdditionalInfo(RoundEndInfoDisplayBlock block)
        {
            var control = MakeBlocks(block);
            _summaryContentContainer?.AddChild(control);
        }

        /// <summary>
        /// Creates a styled panel containing a title and summary body for a single info block.
        /// Used to display grouped round-end statistics such as kills, economy, or deaths.
        /// </summary>
        private PanelContainer MakeBlocks(RoundEndInfoDisplayBlock block)
        {
            var sectionPanel = new PanelContainer
            {
                PanelOverride = new StyleBoxFlat
                {
                    BackgroundColor = block.Color,
                    BorderColor = new Color(70, 70, 70),
                    BorderThickness = new Thickness(2),
                    ContentMarginTopOverride = 6,
                    ContentMarginBottomOverride = 6,
                    ContentMarginRightOverride = 6,
                    ContentMarginLeftOverride = 6,
                },
                Margin = new Thickness(5),
            };

            var content = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
            };

            var titleLabel = new Label
            {
                Text = block.Title,
                FontColorOverride = Color.Gold,
                StyleClasses = { "LabelHeading" },
            };

            var bodyLabel = new RichTextLabel
            {
                Text = block.Body,
                Margin = new Thickness(0, 4, 0, 0),
            };

            content.AddChild(titleLabel);
            content.AddChild(bodyLabel);
            sectionPanel.AddChild(content);

            return sectionPanel;
        }

        /// <summary>
        /// Creates and returns a dedicated panel containing detailed information
        /// about antagonist purchases made during the round, sorted by TC usage.
        /// </summary>
        private Control MakeAntagItemsSection()
        {
            if (_roundEndInfo.AntagItems.Count == 0)
                return new Control();

            var section = new PanelContainer
            {
                PanelOverride = new StyleBoxFlat
                {
                    BackgroundColor = new Color(30, 30, 30, 200),
                    BorderColor = new Color(80, 0, 0),
                    BorderThickness = new Thickness(2),
                },
                Margin = new Thickness(6),
            };

            var content = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                SeparationOverride = 6
            };

            content.AddChild(new RichTextLabel
            {
                Text = Loc.GetString("additional-info-antag-items-label"),
                Modulate = Color.OrangeRed,
                StyleClasses = { "LabelHeading" },
            });

            foreach (var data in _roundEndInfo.AntagItems.OrderByDescending(p => p.TotalTC))
            {
                var playerBox = new BoxContainer()
                {
                    Orientation = LayoutOrientation.Vertical,
                };

                playerBox.AddChild(new Label
                {
                    Text = data.Name,
                    FontColorOverride = Color.Red,
                    StyleClasses = { "LabelBig" },
                });

                playerBox.AddChild(new RichTextLabel
                {
                    Text = Loc.GetString("additional-info-antag-total-spent-tc", ("value", data.TotalTC))
                });

                var iconGrid = new GridContainer
                {
                    Columns = 8,
                    Margin = new Thickness(0, 4, 0, 4),
                };

                foreach (var item in data.ItemPrototypes)
                {
                    if (!_proto.TryIndex<ListingPrototype>(item, out var proto))
                        continue;

                    if (string.IsNullOrEmpty(proto.Name))
                        continue;

                    var icon = new EntityPrototypeView
                    {
                        Scale = new Vector2(1.25f),
                        MinSize = new Vector2(32, 32),
                        MouseFilter = MouseFilterMode.Stop,
                        ToolTip = Loc.GetString(proto.Name),
                    };

                    icon.SetPrototype(proto.ProductEntity);
                    iconGrid.AddChild(icon);
                }

                playerBox.AddChild(iconGrid);
                content.AddChild(playerBox);
            }

            section.AddChild(content);
            return section;
        }
        //ss220 add additional info for round end

        private BoxContainer MakePlayerManifestTab(RoundEndMessageEvent.RoundEndPlayerInfo[] playersInfo)
        {
            var playerManifestTab = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Name = Loc.GetString("round-end-summary-window-player-manifest-tab-title")
            };

            var playerInfoContainerScrollbox = new ScrollContainer
            {
                VerticalExpand = true,
                Margin = new Thickness(10)
            };
            var playerInfoContainer = new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical
            };

            //Put observers at the bottom of the list. Put antags on top.
            var sortedPlayersInfo = playersInfo.OrderBy(p => p.Observer).ThenBy(p => !p.Antag);

            //Create labels for each player info.
            foreach (var playerInfo in sortedPlayersInfo)
            {
                var hBox = new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                };

                var playerInfoText = new RichTextLabel
                {
                    VerticalAlignment = VAlignment.Center,
                    VerticalExpand = true,
                };

                if (playerInfo.PlayerNetEntity != null)
                {
                    hBox.AddChild(new SpriteView(playerInfo.PlayerNetEntity.Value, _entityManager)
                        {
                            OverrideDirection = Direction.South,
                            VerticalAlignment = VAlignment.Center,
                            SetSize = new Vector2(32, 32),
                            VerticalExpand = true,
                        });
                }

                if (playerInfo.PlayerICName != null)
                {
                    if (playerInfo.Observer)
                    {
                        playerInfoText.SetMarkup(
                            Loc.GetString("round-end-summary-window-player-info-if-observer-text",
                                          ("playerOOCName", playerInfo.PlayerOOCName),
                                          ("playerICName", playerInfo.PlayerICName)));
                    }
                    else
                    {
                        //TODO: On Hover display a popup detailing more play info.
                        //For example: their antag goals and if they completed them sucessfully.
                        var icNameColor = playerInfo.Antag ? "red" : "white";
                        playerInfoText.SetMarkup(
                            Loc.GetString("round-end-summary-window-player-info-if-not-observer-text",
                                ("playerOOCName", playerInfo.PlayerOOCName),
                                ("icNameColor", icNameColor),
                                ("playerICName", playerInfo.PlayerICName),
                                ("playerRole", Loc.GetString(playerInfo.Role))));
                    }
                }
                hBox.AddChild(playerInfoText);
                playerInfoContainer.AddChild(hBox);
            }

            playerInfoContainerScrollbox.AddChild(playerInfoContainer);
            playerManifestTab.AddChild(playerInfoContainerScrollbox);

            return playerManifestTab;
        }
    }

}
