using Content.Server.Mind.Components;
namespace Content.Server.SS220.TraitorComponentTarget
{
    [RegisterComponent]
    public sealed class TraitorTargetComponent : Component
    {
        [ViewVariables]
        public bool IsTarget = false;
        public bool CanBeTarget = true;
        public Mind.Mind? Killer = null;
    }
}
