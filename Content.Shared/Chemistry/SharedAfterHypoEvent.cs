namespace Content.Shared.Chemistry
{
    [ByRefEvent]
    public readonly struct AfterHypoEvent
    {
        public readonly EntityUid Hypo;
        public readonly EntityUid Target;
        public readonly EntityUid User;

        public AfterHypoEvent(EntityUid hypo, EntityUid target, EntityUid user)
        {
            Hypo = hypo;
            Target = target;
            User = user;
        }
    }
}
