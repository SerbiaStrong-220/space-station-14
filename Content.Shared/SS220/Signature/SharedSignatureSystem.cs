// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.IO;
using Content.Shared.Paper;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.SS220.Signature;

public abstract class SharedSignatureSystem : EntitySystem
{
    [Dependency] private readonly ISerializationManager _serMan = default!;

    private const int MaxDimension = 1024;

    public override void Initialize()
    {
        SubscribeLocalEvent<PaperComponent, SignatureSubmitMessage>(OnSubmitSignature);
    }

    private void OnSubmitSignature(Entity<PaperComponent> ent, ref SignatureSubmitMessage args)
    {
        var signature = EnsureComp<SignatureComponent>(ent);

        var changedSignature = signature.Data == null || !signature.Data.Equals(args.Data);

        if (changedSignature)
        {
            signature.Data = args.Data;
            Dirty(ent.Owner, signature);
        }

        AfterSubmitSignature((ent.Owner, ent.Comp, signature), ref args, changedSignature);
    }

    protected virtual void AfterSubmitSignature(Entity<PaperComponent, SignatureComponent> ent, ref SignatureSubmitMessage args, bool changedSignature) { }

    #region Profile export/import
    public DataNode? ToDataNode(SignatureData signature)
    {
        if (signature.Width <= 0 || signature.Height <= 0 || signature.Width > MaxDimension || signature.Height > MaxDimension)
            return null;

        var export = new SignatureProfileExport
        {
            Signature = signature,
        };

        var dataNode = _serMan.WriteValue(export, alwaysWrite:true, notNullableOverride:true);
        return dataNode;
    }

    public SignatureData FromStream(Stream stream, ICommonSession session)
    {
        using var reader = new StreamReader(stream, EncodingHelpers.UTF8);
        var yamlStream = new YamlStream();
        yamlStream.Load(reader);

        var root = yamlStream.Documents[0].RootNode;

        var export = _serMan.Read<SignatureProfileExport>(root.ToDataNode(), notNullableOverride: true);
        return export.Signature;
    }
    #endregion Profile export/import
}
