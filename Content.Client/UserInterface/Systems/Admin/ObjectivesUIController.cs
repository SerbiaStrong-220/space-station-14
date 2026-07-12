using Content.Client.Administration.Systems;
using Content.Client.CharacterInfo;
using Content.Client.Players.PlayerInfo;
using Content.Client.UserInterface.Systems.Character.Controls;
using Content.Client.UserInterface.Systems.Objectives.Controls;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Network;
using Robust.Shared.Utility;
using System.Linq;
using Content.Shared.SS220.Objectives;
using Robust.Client.Player;
using static Content.Client.CharacterInfo.AntagonistInfoSystem;

namespace Content.Client.UserInterface.Systems.Admin
{
    [UsedImplicitly]
    public sealed class ObjectivesUIController : UIController, IOnSystemChanged<AdminSystem>, IOnSystemChanged<AntagonistInfoSystem>
    {
        private AdminSystem? _adminSystem;
        private IEntityManager? _entityManager;
        private IPlayerManager? _player; // ss220 add custom goals x2

        [UISystemDependency] private readonly AntagonistInfoSystem _antagonistInfo = default!;
        [UISystemDependency] private readonly SpriteSystem _sprite = default!;

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

            var entity = _entityManager?.GetEntity(_adminSystem.PlayerList.Where(x => x.SessionId == sessionId).Select(s => s.NetEntity).FirstOrDefault());
            if (entity != null)
                _antagonistInfo.RequestAntagonistInfo(entity);

            _window.Open();
        }

        private void AntagonistUpdated(AntagonistData data)
        {
            if (_window == null)
            {
                return;
            }

            var (entity, job, objectives, entityName) = data;

            _window.Title = $"{Loc.GetString("character-info-objectives-label")} {entityName}";
            _window.SpriteView.SetEntity(entity);
            _window.NameLabel.Text = entityName;
            _window.SubText.Text = job;
            _window.Objectives.RemoveAllChildren();
            _window.ObjectivesLabel.Visible = objectives.Any();

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
                    conditionControl.ProgressTexture.Texture = _sprite.Frame0(condition.Icon);
                    conditionControl.ProgressTexture.Progress = condition.Progress;
                    var titleMessage = new FormattedMessage();
                    var descriptionMessage = new FormattedMessage();
                    titleMessage.AddText(condition.Title);
                    descriptionMessage.AddText(condition.Description);

                    conditionControl.Title.SetMessage(titleMessage);
                    conditionControl.Description.SetMessage(descriptionMessage);

                    objectiveControl.AddChild(conditionControl);

                    // ss220 add custom goals x2 start
                    var toggleStatusContainer = new BoxContainer
                    {
                        Orientation = BoxContainer.LayoutOrientation.Vertical,
                    };

                    var toggleStatusObjectiveButton = new Button
                    {
                        Text = Loc.GetString("ui-toggle-status-objective-button"),
                    };

                    toggleStatusContainer.AddChild(toggleStatusObjectiveButton);
                    conditionControl.ObjectiveContainer.AddChild(toggleStatusContainer);

                    toggleStatusObjectiveButton.OnPressed += _ =>
                    {
                        var admin = _player?.LocalEntity;
                        if (admin == null)
                            return;

                        var toggleObjectiveStatusEvent = new ToggleObjectiveStatusEvent(
                            EntityManager.GetNetEntity(entity),
                            EntityManager.GetNetEntity(admin.Value),
                            condition);

                        EntityManager.RaisePredictiveEvent(toggleObjectiveStatusEvent);
                    };

                    // ss220 add custom goals x2 end
                }

                _window.Objectives.AddChild(objectiveControl);
            }

            var controls = _antagonistInfo.GetAntagonistInfoControls(entity);
            foreach (var control in controls)
            {
                _window.Objectives.AddChild(control);
            }

            _window.RolePlaceholder.Visible = !controls.Any() && !objectives.Any();
        }

        public void OnSystemLoaded(AdminSystem system)
        {
            _adminSystem = system;
            _entityManager = IoCManager.Resolve<IEntityManager>();
            _player = IoCManager.Resolve<IPlayerManager>(); // ss220 add custom goals x2
        }

        public void OnSystemUnloaded(AdminSystem system)
        {
            _adminSystem = system;
            _entityManager = IoCManager.Resolve<IEntityManager>();
            _player = IoCManager.Resolve<IPlayerManager>(); // ss220 add custom goals x2
        }

        public void OnSystemLoaded(AntagonistInfoSystem system)
        {
            system.OnAntagonistUpdate += AntagonistUpdated;
        }

        public void OnSystemUnloaded(AntagonistInfoSystem system)
        {
            system.OnAntagonistUpdate -= AntagonistUpdated;
        }
    }
}
