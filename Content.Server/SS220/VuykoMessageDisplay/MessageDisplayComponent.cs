using Robust.Shared.Audio;
namespace Content.Server.SS220.MessageDisplay
{
	[RegisterComponent]
	public sealed partial class MessageDisplayComponent : Component
    {
        [DataField("message")]
        public string Message { get; set; } = "Default message.";
		
		[DataField("sound")]
		public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/SS220/VuykoTestMessengerSound/dredd.ogg");
    }
}
