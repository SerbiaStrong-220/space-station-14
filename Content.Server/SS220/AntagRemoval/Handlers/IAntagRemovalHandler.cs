// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
namespace Content.Server.SS220.AntagRemoval.Handlers;

/// <summary>Role-specific removal only. Shared cleanup stays in RemoveAntagSystem.</summary>
public interface IAntagRemovalHandler
{
    bool RemovesMindRole { get; }
    bool CanRemove(EntityUid body, EntityUid mindId, out string reason);
    bool TryRemove(EntityUid body, EntityUid mindId,
        AntagRemovalGrantComponent provenance, AntagGrantRecord grant, out string reason);
}
