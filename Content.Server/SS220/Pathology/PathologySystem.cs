// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Chat.Managers;
using Content.Server.Forensics;
using Content.Shared.Chat;
using Content.Shared.SS220.Pathology;
using Robust.Shared.Player;

namespace Content.Server.SS220.Pathology;

public sealed partial class PathologySystem : SharedPathologySystem
{
    [Dependency] private IChatManager _chat = default!;

    protected override void SendSelfMessage(EntityUid entity, string message, Color? color)
    {
        if (!TryComp<ActorComponent>(entity, out var actor))
            return;

        var wrapped = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
        // we only show this in chat
        _chat.ChatMessageToOne(ChatChannel.Emotes, message, wrapped, EntityUid.Invalid, false, actor.PlayerSession.Channel, color);
    }

    protected override void ApplyPathologyContext(Entity<PathologyHolderComponent> entity, IPathologyContext? context)
    {
        base.ApplyPathologyContext(entity, context);

        switch (context)
        {
            case EntityProvidedPathologyContext entityProvided:
                HandleEntityProvidedContext(entity, entityProvided);
                break;

            default:
                break;
        }
    }

    private void HandleEntityProvidedContext(Entity<PathologyHolderComponent> entity, EntityProvidedPathologyContext context)
    {
        if (!TrySpawnNextTo(context.ProtoId, entity.Owner, out var spawnedUid))
            return;

        var forensicsComponent = EnsureComp<ForensicsComponent>(spawnedUid.Value);

        forensicsComponent.Fingerprints.UnionWith(context.Fingerprints);
        forensicsComponent.DNAs.UnionWith(context.DNAs);
    }
}
