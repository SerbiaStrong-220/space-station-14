// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.SS220.CultYogg.MiGo.UI;
using Content.Shared.SS220.CultYogg.Buildings;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Placement;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;


namespace Content.Client.SS220.CultYogg.MiGo;

public sealed class MiGoErectPlacementHijack : PlacementHijack
{
        private readonly IComponentFactory _componentFactory;


        public override bool CanRotate { get; }


        public override bool HijackPlacementRequest(EntityCoordinates coordinates)
        {

        }


        {
            _presenter.SendEntity(ent);
            return true;
        }

    }
}
