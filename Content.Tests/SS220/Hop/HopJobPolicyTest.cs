using Content.Shared.SS220.Hop;
using NUnit.Framework;

namespace Content.Tests.SS220.Hop;

[TestFixture]
public sealed class HopJobPolicyTest
{
    [TestCase(1, 0, 1)]
    [TestCase(2, 0, 3)]
    [TestCase(3, 0, 4)]
    [TestCase(4, 1, 5)]
    [TestCase(4, 6, 0)]
    public void IncreaseIsBoundedByOriginalStaffing(int baseline, int occupied, int expected)
    {
        Assert.That(HopJobPolicy.MaximumAvailable(baseline, occupied), Is.EqualTo(expected));
    }

    [Test]
    public void UnlimitedAndEventLocksAreEnforced()
    {
        Assert.That(HopJobPolicy.CanSetSlots("Captain", null, 1, 1, false), Is.False);
        Assert.That(HopJobPolicy.CanSetSlots("Passenger", null, 1, 10, false), Is.True);
        Assert.That(HopJobPolicy.CanSetSlots("Captain", 2, 1, 1, false), Is.False);
        Assert.That(HopJobPolicy.CanSetSlots("Captain", 0, null, 1, true), Is.False);
        Assert.That(HopJobPolicy.CanSetSlots("Captain", 1, 1, 1, true), Is.True);
        Assert.That(HopJobPolicy.CanSetSlots("Captain", null, null, 1, true), Is.True);
    }

    [Test]
    public void MissingSlotsAreClosedNotUnlimited()
    {
        Assert.That(HopJobPolicy.AvailableSlots(false, null), Is.EqualTo(0));
        Assert.That(HopJobPolicy.AvailableSlots(true, null), Is.Null);
        Assert.That(HopJobPolicy.AvailableSlots(true, 1), Is.EqualTo(1));
        Assert.That(HopJobPolicy.AvailableSlots(true, 0), Is.EqualTo(0));
    }

    [Test]
    public void SpecialJobsAreExcludedEvenOnSpawnAllJobsMaps()
    {
        Assert.That(HopJobPolicy.IsCrewJob(false, [-1, -1], true), Is.False);
        Assert.That(HopJobPolicy.IsCrewJob(true, [1, 1], true), Is.True);
        Assert.That(HopJobPolicy.IsCrewJob(true, [1, 0], false), Is.True);
        Assert.That(HopJobPolicy.IsCrewJob(true, [0, 1], true), Is.True);
        Assert.That(HopJobPolicy.IsCrewJob(true, null, true), Is.True);
        Assert.That(HopJobPolicy.IsCrewJob(true, [0, 0], false), Is.False);
        Assert.That(HopJobPolicy.IsCrewJob(true, [0, 0], true, 0), Is.False);
        Assert.That(HopJobPolicy.IsCrewJob(true, [1, 1], true, 0), Is.True);
    }

    [Test]
    public void ConfiguredFiniteLimitsStayFinite()
    {
        Assert.That(HopJobPolicy.ConfiguredSlots([1, 1], 0), Is.EqualTo(1));
        Assert.That(HopJobPolicy.ConfiguredSlots([1, 1], 1), Is.EqualTo(1));
        Assert.That(HopJobPolicy.ConfiguredSlots([-1, -1], 1), Is.Null);
        Assert.That(HopJobPolicy.ConfiguredSlots(null, 1), Is.EqualTo(0));
        Assert.That(HopJobPolicy.ValidSlots(-1), Is.False);
        Assert.That(HopJobPolicy.ValidSlots(1001), Is.False);
    }
}
