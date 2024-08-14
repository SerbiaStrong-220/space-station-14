using Robust.Shared.GameStates;

namespace Content.Shared.SS220.ShowCodewords;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ShowCodewordsComponent : Component
{
    [DataField("codewords"), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public string[] CodeWords;
}
