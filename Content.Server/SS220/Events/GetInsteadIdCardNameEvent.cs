// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
namespace Content.Server.SS220.Events;

public sealed class GetInsteadIdCardNameEvent : EntityEventArgs
{
    public EntityUid Uid;
    public string? Name;

    public GetInsteadIdCardNameEvent(EntityUid uid)
    {
        Uid = uid;
    }
}
