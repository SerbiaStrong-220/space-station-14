using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Containers.ItemSlots;

namespace Content.Shared.SS220.PkaModification
{
    [RegisterComponent]
    public sealed partial class PkaModificationComponent : Component
    {
        [DataField("modificationSlot1", required: true)]
        public ItemSlot ModificationSlot1 = new();

        [DataField("modificationSlot2", required: true)]
        public ItemSlot ModificationSlot2 = new();

        [DataField("modificationSlot3", required: true)]
        public ItemSlot ModificationSlot3 = new();

        [DataField("modificationSlot4", required: true)]
        public ItemSlot ModificationSlot4 = new();

        [ViewVariables]
        public int CountDamage = 0;

        [ViewVariables]
        public int CountRange = 0;

        [ViewVariables]
        public int CountCooldown = 0;

        public float StartCooldownTime = 0;

        public float ChangePerCDModule = 0.25f;
    }
}
