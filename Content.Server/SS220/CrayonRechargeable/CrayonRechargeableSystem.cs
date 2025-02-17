using Content.Server.Crayon;
using Content.Shared.SS220.CrayonRechargeable;
using Robust.Shared.Timing;

namespace Content.Server.SS220.CrayonRechargeable
{
    public sealed class CrayonRechargeableSystem : EntitySystem
    {
        [Dependency] private readonly IGameTiming _timing = default!;

        private bool IsChargeAvailable(CrayonComponent crayonComp, CrayonRechargeableComponent rechargeableComp, TimeSpan curTime)
        {
            if (crayonComp.Capacity > crayonComp.Charges && curTime >= rechargeableComp.NextChargeTime)
                return true;

            return false;
        }

        private void Charge(EntityUid uid, CrayonComponent crayonComp, CrayonRechargeableComponent rechargeableComp, TimeSpan curTime)
        {
            crayonComp.Charges = Math.Clamp(crayonComp.Charges + rechargeableComp.ChargesPerWait, 0, crayonComp.Capacity);
            rechargeableComp.NextChargeTime = rechargeableComp.WaitingForCharge + curTime;

            Dirty(uid, crayonComp);
        }

        public override void Update(float frameTime)
        {
            var query = EntityQueryEnumerator<CrayonComponent, CrayonRechargeableComponent>();
            var curTime = _timing.CurTime;

            while (query.MoveNext(out var uid, out var crayonComp, out var rechargeableComp))
                if (IsChargeAvailable(crayonComp, rechargeableComp, curTime))
                    Charge(uid, crayonComp, rechargeableComp, curTime);
        }
    }
}
