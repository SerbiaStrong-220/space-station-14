using Content.Shared.SS220.DarkForces.Narsi.Buildings;
using Content.Shared.SS220.DarkForces.Narsi.Roles;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Client.SS220.DarkForces.Narsi.Buildings;

public sealed class ClientFakeNarsiDoorSystem : EntitySystem
{
    [Dependency] private readonly IResourceCache _cache = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SharedFakeNarsiDoorComponent, ComponentInit>(onFakeNarsiDoorInitialize);
        SubscribeLocalEvent<SharedFakeNarsiDoorComponent, ComponentHandleState>(OnAppearanceHandleState);

        SubscribeLocalEvent<NarsiCultistComponent, ComponentInit>(OnNarsiCultistInit);
        SubscribeLocalEvent<NarsiCultistComponent, ComponentRemove>(OnNarsiCultistShutdown);
    }

    private void OnNarsiCultistShutdown(EntityUid uid, NarsiCultistComponent component, ComponentRemove args)
    {
        var fakeNarsiDoors = EntityQueryEnumerator<SharedFakeNarsiDoorComponent>();
        while (fakeNarsiDoors.MoveNext(out var doorUid, out var fakeNarsiDoorComponent))
        {
            SetAirlockRsi(doorUid, fakeNarsiDoorComponent, fakeNarsiDoorComponent.FakeRsiPath, fakeNarsiDoorComponent.RealRsiPath);
        }
    }

    private void OnNarsiCultistInit(EntityUid uid, NarsiCultistComponent component, ComponentInit args)
    {
        var fakeNarsiDoors = EntityQueryEnumerator<SharedFakeNarsiDoorComponent>();
        while (fakeNarsiDoors.MoveNext(out var doorUid, out var fakeNarsiDoorComponent))
        {
            SetAirlockRsi(doorUid, fakeNarsiDoorComponent, fakeNarsiDoorComponent.FakeRsiPath, fakeNarsiDoorComponent.RealRsiPath);
        }
    }

    private void OnAppearanceHandleState(EntityUid uid, SharedFakeNarsiDoorComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not SharedFakeNarsiDoorComponentState state)
            return;

        SetAirlockRsi(uid, component, state.FakeRsiPath, state.RealRsiPath);
    }

    private void onFakeNarsiDoorInitialize(EntityUid uid, SharedFakeNarsiDoorComponent component, ComponentInit ev)
    {
        SetAirlockRsi(uid, component, component.FakeRsiPath, component.RealRsiPath);
    }

    private void SetAirlockRsi(EntityUid doorUid, SharedFakeNarsiDoorComponent component, string fakePath, string realPath)
    {
        var attached = _player.LocalPlayer?.ControlledEntity;
        if (attached == null)
            return;

        string actualPath = fakePath;

        if (HasComp<NarsiCultistComponent>(attached))
            actualPath = realPath;

        var rsi = _cache.GetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / actualPath).RSI;
        if (rsi == null || !TryComp<SpriteComponent>(doorUid, out var sprite))
            return;

        sprite.BaseRSI = rsi;
    }
}
