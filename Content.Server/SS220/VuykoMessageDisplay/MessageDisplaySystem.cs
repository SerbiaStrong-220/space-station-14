using Content.Server.SS220.MessageDisplay;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Server.Popups;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.SS220.MessageDisplay
{
    public sealed partial class MessageDisplaySystem : EntitySystem
    {
        [Dependency] protected readonly SharedAppearanceSystem _appearanceSystem = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
		[Dependency] private readonly AudioSystem _audio = default!;

        public override void Initialize()
        {
            SubscribeLocalEvent<MessageDisplayComponent, UseInHandEvent>(OnUseInHand);
        }

        private void OnUseInHand(EntityUid user, MessageDisplayComponent component, UseInHandEvent args)
        {
            _popupSystem.PopupEntity(component.Message, user);
			_audio.PlayPvs(component.Sound, user);
        }
    }
}
