using Content.Client.Administration.Systems;
using Content.Client.CharacterInfo;
using Content.Client.Players.PlayerInfo;
using Content.Client.UserInterface.Systems.Character.Controls;
using Content.Client.UserInterface.Systems.Objectives.Controls;
using Content.Shared.CharacterInfo;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.Network;
using Robust.Shared.Utility;
using System.Linq;
using static Content.Client.CharacterInfo.CharacterInfoSystem;

namespace Content.Client.UserInterface.Systems.Admin
{
    [UsedImplicitly]
    public sealed class ObjectivesUIController : UIController, IOnSystemChanged<AdminSystem>, IOnSystemChanged<CharacterInfoSystem>
    {
        private AdminSystem? _adminSystem;
        [UISystemDependency] private readonly CharacterInfoSystem _characterInfo = default!;

        private ObjectivesWindow? _window = default!;

        private void EnsureWindow()
        {
            if (_window is { Disposed: false })
                return;

            _window = UIManager.CreateWindow<ObjectivesWindow>();
            LayoutContainer.SetAnchorPreset(_window, LayoutContainer.LayoutPreset.CenterTop);
        }

        public void OpenWindow(NetUserId sessionId)
        {
            EnsureWindow();

            if (_window == null || _adminSystem == null)
            {
                return;
            }

            _characterInfo.RequestAntagonistInfo(_adminSystem.PlayerList.Where(x => x.SessionId == sessionId).Select(s =>s.EntityUid).FirstOrDefault());//_adminSystem.PlayerList.Where(x => x.SessionId == sessionId).FirstOrDefault();
            _window.Open();
        }

        

        private void AntagonistUpdated(CharacterData data)
        {
            if (_window == null)
            {
                return;
            }

            var (job, objectives, briefing, sprite, entityName) = data;

            if (objectives == null)
            {
                return;
            }

            _window.SubText.Text = job;
            _window.Objectives.RemoveAllChildren();

            _window.Title = $"{Loc.GetString("character-info-objectives-label")} {entityName}";

            foreach (var (groupId, conditions) in objectives)
            {
                var objectiveControl = new CharacterObjectiveControl
                {
                    Orientation = BoxContainer.LayoutOrientation.Vertical,
                    Modulate = Color.Gray
                };

                objectiveControl.AddChild(new Label
                {
                    Text = groupId,
                    Modulate = Color.LightSkyBlue
                });

                foreach (var condition in conditions)
                {
                    var conditionControl = new ObjectiveConditionsControl();
                    conditionControl.ProgressTexture.Texture = condition.SpriteSpecifier.Frame0();
                    conditionControl.ProgressTexture.Progress = condition.Progress;
                    var titleMessage = new FormattedMessage();
                    var descriptionMessage = new FormattedMessage();
                    titleMessage.AddText(condition.Title);
                    descriptionMessage.AddText(condition.Description);

                    conditionControl.Title.SetMessage(titleMessage);
                    conditionControl.Description.SetMessage(descriptionMessage);

                    objectiveControl.AddChild(conditionControl);
                }

                var briefingControl = new ObjectiveBriefingControl();
                briefingControl.Label.Text = briefing;

                objectiveControl.AddChild(briefingControl);
                _window.Objectives.AddChild(objectiveControl);
            }

            _window.SpriteView.Sprite = sprite;
            _window.NameLabel.Text = entityName;
        }





        public void OnSystemLoaded(CharacterInfoSystem system)
        {
            system.OnAntagonistUpdate += AntagonistUpdated;
        }

        public void OnSystemUnloaded(CharacterInfoSystem system)
        {
            system.OnAntagonistUpdate -= AntagonistUpdated;
        }

        public void OnSystemLoaded(AdminSystem system)
        {
            _adminSystem = system;
        }

        public void OnSystemUnloaded(AdminSystem system)
        {
            _adminSystem = system;
        }
    }
}
