// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Virology;

[ImplicitDataDefinitionForInheritors]
public abstract partial class VirusProgressCondition
{
    [DataField]
    public bool Invert;

    protected abstract bool Condition(in VirusProgressArgs args);

    public bool CheckCondition(in VirusProgressArgs args)
    {
        return Invert != Condition(in args);
    }
}
