namespace Content.Shared.SS220.Signature;

[DataDefinition]
public sealed partial class SignatureProfileExport
{
    [DataField(required: true)]
    public SignatureData Signature = default!;
}
