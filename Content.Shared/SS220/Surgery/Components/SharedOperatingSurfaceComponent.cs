using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Surgery
{
    [NetSerializable()]
    public abstract partial class SharedOperatingSurfaceComponent : Component
    {
        /// <summary>
        /// От успеха операции также зависит и то, где она проводится. Но на данный момент оно нам и нахой не нужно: делаем нарезки хоть на табурете
        /// </summary>

        [DataField("operationSuccesfulChance")]
        public float OperationSuccesfulChance = 100f;
    }
}
