namespace Content.Server.SS220.AntagItem
{
    [RegisterComponent]
    public sealed partial class AntagStorageComponent : Component
    {
        [DataField("slot")]
        public string Slot = string.Empty;
    }
}
