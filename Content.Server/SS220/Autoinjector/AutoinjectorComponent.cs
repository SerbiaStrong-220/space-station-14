namespace Content.Server.Chemistry.Components
{
    [RegisterComponent]
    public sealed partial class AutoinjectorComponent : Component
    {
        [DataField]
        public string OnUseMessage = "loc-autoinjector-after-use";
        [DataField]
        public string OnExaminedMessage = "loc-autoinjector-examined-message";
        [DataField, ViewVariables(VVAccess.ReadOnly)]
        public bool Used = false;
    }
}
