namespace Content.Server.SS220.RecentlyUsedNarcotics;

[RegisterComponent]
public sealed partial class RecentlyUsedNarcoticsComponent : Component
{
    [DataField]
    public TimeSpan LastTimeUsedNarcotics;

    [DataField]
    public TimeSpan AddTimeForOneUse = TimeSpan.FromMinutes(10);

    [DataField]
    public TimeSpan TimeRemoveNarcoticsFromBlood;
}
