// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

namespace Content.Shared.SS220.MindShield;

[ByRefEvent]
public readonly record struct MindshieldProtectionGrantedEvent
{
    public readonly EntityUid Implant;
    public readonly EntityUid Implanted;

    public MindshieldProtectionGrantedEvent(EntityUid implant, EntityUid implanted)
    {
        Implant = implant;
        Implanted = implanted;
    }
}

[ByRefEvent]
public readonly record struct MindshieldProtectionRemovedEvent
{
    public readonly EntityUid Implant;
    public readonly EntityUid Implanted;

    public MindshieldProtectionRemovedEvent(EntityUid implant, EntityUid implanted)
    {
        Implant = implant;
        Implanted = implanted;
    }
}
