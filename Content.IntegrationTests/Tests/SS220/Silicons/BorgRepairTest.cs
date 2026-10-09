using System.Linq;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Repairable;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Tools.Systems;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.SS220.Silicons;

[TestOf(typeof(RepairableSystem))]
public sealed class BorgRepairTest : InteractionTest
{
    protected override string PlayerPrototype => "BorgRepairTestPlayer";

    [TestPrototypes]
    private const string Prototypes = """
        - type: entity
          id: BorgRepairTestPlayer
          parent: InteractionTestMob
          components:
          - type: ContainerContainer
          - type: BorgChassis
            hasMindState: robot_e
            noMindState: robot_e_r
          - type: Appearance
          - type: Sprite
            sprite: Mobs/Silicon/chassis.rsi
            layers:
            - state: robot
            - state: robot_e_r
              map: ["enum.BorgVisualLayers.Light"]

        - type: entity
          id: BorgRepairTestWelder
          parent: Welder
          components:
          - type: Welder
            fuelConsumption: 0
            fuelLitCost: 0
        """;

    private DamageableSystem Damage => SEntMan.System<DamageableSystem>();

    private async Task<EntityUid> PrepareTarget(bool selfRepair, bool borgUser = true)
    {
        await SpawnTarget("BorgChassisGeneric");
        var target = STarget.Value;
        var config = SEntMan.GetComponent<RepairableComponent>(target);
        Assert.Multiple(() =>
        {
            Assert.That(config.DamageValue, Is.EqualTo(-10f));
            Assert.That(config.DoAfterDelay, Is.EqualTo(1));
            Assert.That(config.FuelCost, Is.EqualTo(0.5f));
            Assert.That(config.AllowSelfRepair, Is.True);
        });

        await Server.WaitPost(() =>
        {
            if (!borgUser)
                SEntMan.RemoveComponent<BorgChassisComponent>(SPlayer);

            if (!selfRepair)
                return;

            target = SPlayer;
            SEntMan.EnsureComponent<DamageableComponent>(target);
            var repairable = SEntMan.EnsureComponent<RepairableComponent>(target);
            repairable.DamageValue = config.DamageValue;
            repairable.DoAfterDelay = config.DoAfterDelay;
            repairable.FuelCost = config.FuelCost;
            repairable.AllowSelfRepair = config.AllowSelfRepair;
        });
        return target;
    }

    private async Task ChangeDamage(EntityUid target, float amount)
    {
        await Server.WaitPost(() => Damage.TryChangeDamage(target,
            new DamageSpecifier { DamageDict = { ["Blunt"] = FixedPoint2.New(amount) } },
            ignoreResistances: true));
    }

    private async Task StartRepair(EntityUid target, EntityUid tool, EntityUid? user = null)
    {
        await Server.WaitPost(() =>
        {
            var ev = new InteractUsingEvent(user ?? SPlayer, tool, target,
                SEntMan.GetComponent<TransformComponent>(target).Coordinates);
            SEntMan.EventBus.RaiseLocalEvent(target, ev);
            Assert.That(ev.Handled, Is.True);
        });
    }

    private Content.Shared.DoAfter.DoAfter ActiveRepair()
    {
        return SEntMan.GetComponent<DoAfterComponent>(SPlayer).DoAfters.Values
            .Single(d => !d.Completed && !d.Cancelled);
    }

    private void AssertNoActiveRepair()
    {
        Assert.That(SEntMan.GetComponent<DoAfterComponent>(SPlayer).DoAfters.Values
            .Any(d => !d.Completed && !d.Cancelled), Is.False);
    }

    [TestCase(10f, false)]
    [TestCase(90f, false)]
    [TestCase(25f, false)]
    [TestCase(10f, true)]
    [TestCase(90f, true)]
    [TestCase(25f, true)]
    public async Task BorgRepairsOnlyHalf(float damage, bool selfRepair)
    {
        var target = await PrepareTarget(selfRepair);
        var tool = ToServer(await PlaceInHands("BorgRepairTestWelder"));
        await ChangeDamage(target, damage);
        var fuelBefore = ToolSys.GetWelderFuelAndCapacity(tool).fuel;

        await StartRepair(target, tool);
        var firstAmount = Math.Min(damage / 2, 10);
        var penalty = selfRepair ? 3 : 1;
        Assert.That(ActiveRepair().Args.Delay.TotalSeconds,
            Is.EqualTo(firstAmount / 10 * penalty * ActiveRepair().Args.DelayModifier).Within(0.001));
        Assert.That(((SharedToolSystem.ToolDoAfterEvent) ActiveRepair().Args.Event).Fuel,
            Is.EqualTo(firstAmount / 10 * 0.5f).Within(0.001));

        await RunSeconds(damage / 20 * penalty + 2);
        Assert.That(Damage.GetTotalDamage(target), Is.EqualTo(FixedPoint2.New(damage / 2)));
        Assert.That(fuelBefore - ToolSys.GetWelderFuelAndCapacity(tool).fuel,
            Is.EqualTo(FixedPoint2.New(damage / 20 * 0.5f)));
        AssertNoActiveRepair();

        // Restarting cannot halve the remaining damage again.
        await StartRepair(target, tool);
        AssertNoActiveRepair();
        Assert.That(Damage.GetTotalDamage(target), Is.EqualTo(FixedPoint2.New(damage / 2)));

        // A different borg cannot reset the target's repair budget either.
        EntityUid otherBorg = default;
        await Server.WaitPost(() => otherBorg = SEntMan.SpawnEntity("BorgRepairTestPlayer",
            SEntMan.GetComponent<TransformComponent>(SPlayer).Coordinates));
        await StartRepair(target, tool, otherBorg);
        Assert.That(SEntMan.GetComponent<DoAfterComponent>(otherBorg).DoAfters, Is.Empty);
    }

    [Test]
    public async Task HumanFinishesRepairAndNewDamageGetsNewBudget()
    {
        var target = await PrepareTarget(false);
        var tool = ToServer(await PlaceInHands("BorgRepairTestWelder"));
        await ChangeDamage(target, 25);
        await StartRepair(target, tool);
        await RunSeconds(4);
        Assert.That(Damage.GetTotalDamage(target), Is.EqualTo(FixedPoint2.New(12.5f)));

        await Server.WaitPost(() => SEntMan.RemoveComponent<BorgChassisComponent>(SPlayer));
        await StartRepair(target, tool);
        await RunSeconds(4);
        Assert.That(Damage.GetTotalDamage(target), Is.EqualTo(FixedPoint2.Zero));
        Assert.That(SEntMan.GetComponent<RepairableComponent>(target).UnrepairableDamage,
            Is.EqualTo(FixedPoint2.Zero));

        await ChangeDamage(target, 10);
        Assert.That(SEntMan.GetComponent<RepairableComponent>(target).UnrepairableDamage,
            Is.EqualTo(FixedPoint2.New(5)));
    }

    [Test]
    public async Task FullHumanRepairResetsSelfRepairBudget()
    {
        var target = await PrepareTarget(true);
        var tool = ToServer(await PlaceInHands("BorgRepairTestWelder"));
        var repairable = SEntMan.GetComponent<RepairableComponent>(target);

        await ChangeDamage(target, 50);
        await StartRepair(target, tool);
        await RunSeconds(10);
        Assert.That(Damage.GetTotalDamage(target), Is.EqualTo(FixedPoint2.New(25)));
        Assert.That(repairable.UnrepairableDamage, Is.EqualTo(FixedPoint2.New(25)));
        AssertNoActiveRepair();

        EntityUid human = default;
        EntityUid humanTool = default;
        await Server.WaitPost(() =>
        {
            var coords = SEntMan.GetComponent<TransformComponent>(target).Coordinates;
            human = SEntMan.SpawnEntity("InteractionTestMob", coords);
            humanTool = SEntMan.SpawnEntity("BorgRepairTestWelder", coords);
            Assert.That(HandSys.TryPickup(human, humanTool, "hand_right", false, false, false), Is.True);
            Assert.That(ItemToggleSys.TryActivate(
                (humanTool, SEntMan.GetComponent<ItemToggleComponent>(humanTool)), user: human), Is.True);
        });
        await StartRepair(target, humanTool, human);
        await RunSeconds(4);
        Assert.Multiple(() =>
        {
            Assert.That(Damage.GetTotalDamage(target), Is.EqualTo(FixedPoint2.Zero));
            Assert.That(repairable.UnrepairableDamage, Is.EqualTo(FixedPoint2.Zero));
            Assert.That(repairable.LastDamage, Is.EqualTo(FixedPoint2.Zero));
        });

        await ChangeDamage(target, 10);
        Assert.That(repairable.UnrepairableDamage, Is.EqualTo(FixedPoint2.New(5)));
        var fuelBefore = ToolSys.GetWelderFuelAndCapacity(tool).fuel;
        await StartRepair(target, tool);
        await RunSeconds(3);
        Assert.That(Damage.GetTotalDamage(target), Is.EqualTo(FixedPoint2.New(5)));
        Assert.That(fuelBefore - ToolSys.GetWelderFuelAndCapacity(tool).fuel,
            Is.EqualTo(FixedPoint2.New(0.25f)));
        AssertNoActiveRepair();

        await StartRepair(target, tool);
        AssertNoActiveRepair();
        Assert.That(Damage.GetTotalDamage(target), Is.EqualTo(FixedPoint2.New(5)));
    }

    [Test]
    public async Task FurtherDamageDoesNotUnlockOldDamage()
    {
        var target = await PrepareTarget(false);
        var tool = ToServer(await PlaceInHands("BorgRepairTestWelder"));
        await ChangeDamage(target, 10);
        await StartRepair(target, tool);
        await RunSeconds(2);
        await ChangeDamage(target, 20);
        await StartRepair(target, tool);
        await RunSeconds(3);
        Assert.That(Damage.GetTotalDamage(target), Is.EqualTo(FixedPoint2.New(15)));
        AssertNoActiveRepair();
    }

    [Test]
    public async Task CancelledRepairDoesNotConsumeFuelOrRepairBudget()
    {
        var target = await PrepareTarget(true);
        var tool = ToServer(await PlaceInHands("BorgRepairTestWelder"));
        await ChangeDamage(target, 25);
        var fuelBefore = ToolSys.GetWelderFuelAndCapacity(tool).fuel;
        await StartRepair(target, tool);
        await Server.WaitPost(() => DoAfterSys.Cancel(ActiveRepair().Id));
        await RunSeconds(4);
        Assert.That(Damage.GetTotalDamage(target), Is.EqualTo(FixedPoint2.New(25)));
        Assert.That(ToolSys.GetWelderFuelAndCapacity(tool).fuel, Is.EqualTo(fuelBefore));
        Assert.That(SEntMan.GetComponent<RepairableComponent>(target).UnrepairableDamage,
            Is.EqualTo(FixedPoint2.New(12.5f)));
        AssertNoActiveRepair();
    }

    [TestCase(10f)]
    [TestCase(90f)]
    [TestCase(25f)]
    public async Task HumanCanRepairAllDamage(float damage)
    {
        var target = await PrepareTarget(false, borgUser: false);
        var tool = ToServer(await PlaceInHands("BorgRepairTestWelder"));
        await ChangeDamage(target, damage);
        var fuelBefore = ToolSys.GetWelderFuelAndCapacity(tool).fuel;
        await StartRepair(target, tool);
        await RunSeconds(damage / 10 + 2);
        Assert.That(Damage.GetTotalDamage(target), Is.EqualTo(FixedPoint2.Zero));
        Assert.That(fuelBefore - ToolSys.GetWelderFuelAndCapacity(tool).fuel,
            Is.EqualTo(FixedPoint2.New(damage / 10 * 0.5f)));
        AssertNoActiveRepair();
    }

    [Test]
    public async Task DisablingSelfRepairPreventsRepairingSelf()
    {
        var target = await PrepareTarget(true);
        var tool = ToServer(await PlaceInHands("BorgRepairTestWelder"));
        await Server.WaitPost(() =>
        {
            var repairable = SEntMan.GetComponent<RepairableComponent>(target);
            repairable.AllowSelfRepair = false;
            SEntMan.Dirty(target, repairable);
        });
        await ChangeDamage(target, 25);
        var fuelBefore = ToolSys.GetWelderFuelAndCapacity(tool).fuel;
        await Server.WaitPost(() =>
        {
            var ev = new InteractUsingEvent(SPlayer, tool, target,
                SEntMan.GetComponent<TransformComponent>(target).Coordinates);
            SEntMan.EventBus.RaiseLocalEvent(target, ev);
            Assert.That(ev.Handled, Is.False);
        });
        await RunSeconds(4);
        AssertNoActiveRepair();
        Assert.That(Damage.GetTotalDamage(target), Is.EqualTo(FixedPoint2.New(25)));
        Assert.That(ToolSys.GetWelderFuelAndCapacity(tool).fuel, Is.EqualTo(fuelBefore));
    }

    [Test]
    public async Task OrdinaryRepairableWithSelfRepairEnabledUsesStandardRepair()
    {
        var target = await PrepareTarget(true, borgUser: false);
        var tool = ToServer(await PlaceInHands("BorgRepairTestWelder"));
        await ChangeDamage(target, 25);
        await StartRepair(target, tool);
        Assert.That(((SharedToolSystem.ToolDoAfterEvent) ActiveRepair().Args.Event).WrappedEvent,
            Is.TypeOf<RepairDoAfterEvent>());
        await RunSeconds(12);
        Assert.That(Damage.GetTotalDamage(target), Is.EqualTo(FixedPoint2.Zero));
        Assert.That(SEntMan.GetComponent<RepairableComponent>(target).UnrepairableDamage,
            Is.EqualTo(FixedPoint2.Zero));
        AssertNoActiveRepair();
    }
}
