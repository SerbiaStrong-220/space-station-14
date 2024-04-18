using Content.Server.Cargo.Components;
using Content.Server.Cargo.Systems;
using Content.Shared.Cargo.Components;
using Content.Shared.SS220.Cargo.Events;
using Content.Server.Station.Systems;
using Content.Shared.Cargo;
using Robust.Shared.Audio.Systems;
using Content.Shared.Stacks;
using Content.Server.Stack;
using Robust.Shared.Prototypes;


namespace Content.Server.SS220.Cargо
{
    public sealed partial class CargoCashOutSystem : SharedCargoSystem
    {
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] private readonly StationSystem _station = default!;
        [Dependency] private readonly IPrototypeManager _protoMan = default!;
        [Dependency] private readonly StackSystem _stack = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<CargoOrderConsoleComponent, CargoConsoleCashOutMessage>(CashOut);
        }

        private void CashOut(EntityUid uid, CargoOrderConsoleComponent component, ref CargoConsoleCashOutMessage arg)
        {
            var stationUid = _station.GetOwningStation(uid);

            if (arg.Amount == 0)
                return;

            if (!TryComp(stationUid, out StationBankAccountComponent? bank))
                return;

            if (bank == null)
                return;

            if (arg.Amount > bank.Balance)
            {
                _audio.PlayPvs(_audio.GetSound(component.ErrorSound), uid);
                return;
            }

            var xform = Transform(uid);
            var stackPrototype = _protoMan.Index<StackPrototype>("Credit");
            _stack.Spawn(arg.Amount, stackPrototype, xform.Coordinates);

            var cargoSystem = _entitySystemManager.GetEntitySystem<CargoSystem>();
            cargoSystem.UpdateBankAccount(stationUid.Value, bank, -arg.Amount);
            _audio.PlayPvs(component.ConfirmSound, uid);
        }
    }
}
