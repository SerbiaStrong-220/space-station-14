using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared.SS220.PkaModification
{
    [RegisterComponent]
    public sealed partial class PkaModificationComponent : Component
    {
        [ViewVariables]
        public EntityUid SavedUid;
    }
}
