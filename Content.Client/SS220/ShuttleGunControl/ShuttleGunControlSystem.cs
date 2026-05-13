using Content.Shared.SS220.ShuttleGunControl;
using Content.Shared.SS220.Input;
using Robust.Client.Input;

namespace Content.Client.SS220.ShuttleGunControl;

public sealed class ShuttleGunControlSystem : SharedShuttleGunControlSystem
{
    [Dependency] private readonly IInputManager _input = default!;

    private const string ShuttleGunControlContext = "additionalShuttle";
    private const string HumanContext = "human";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShuttleGunControlComponent, BoundUIOpenedEvent>(OnBoundUIOpened);
        SubscribeLocalEvent<ShuttleGunControlComponent, BoundUIClosedEvent>(OnBoundUIClosed);

        var shuttleGunControlContext = _input.Contexts.New(ShuttleGunControlContext, HumanContext);
        shuttleGunControlContext.AddFunction(KeyFunctions220.FireShuttle);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _input.Contexts.Remove(ShuttleGunControlContext);
    }

    private void OnBoundUIOpened(Entity<ShuttleGunControlComponent> ent, ref BoundUIOpenedEvent args)
    {
        _input.Contexts.SetActiveContext(ShuttleGunControlContext);
    }

    private void OnBoundUIClosed(Entity<ShuttleGunControlComponent> ent, ref BoundUIClosedEvent args)
    {
        _input.Contexts.SetActiveContext(HumanContext);
    }
}
