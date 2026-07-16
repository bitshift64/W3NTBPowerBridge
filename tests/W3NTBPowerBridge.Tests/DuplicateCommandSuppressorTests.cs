using W3NTBPowerBridge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace W3NTBPowerBridge.Tests;

[TestClass]
public sealed class DuplicateCommandSuppressorTests
{
    [TestMethod]
    public void ShouldSuppress_SameFrequencyInsideWindow_ReturnsTrue()
    {
        var suppressor = new DuplicateCommandSuppressor(TimeSpan.FromMilliseconds(500));
        var now = DateTimeOffset.Parse("2026-07-16T12:00:00Z");

        Assert.IsFalse(suppressor.ShouldSuppress(14_250_000, now));
        Assert.IsTrue(suppressor.ShouldSuppress(14_250_000, now.AddMilliseconds(499)));
    }

    [TestMethod]
    public void ShouldSuppress_SameFrequencyOutsideWindow_ReturnsFalse()
    {
        var suppressor = new DuplicateCommandSuppressor(TimeSpan.FromMilliseconds(500));
        var now = DateTimeOffset.Parse("2026-07-16T12:00:00Z");

        Assert.IsFalse(suppressor.ShouldSuppress(14_250_000, now));
        Assert.IsFalse(suppressor.ShouldSuppress(14_250_000, now.AddMilliseconds(500)));
    }

    [TestMethod]
    public void ShouldSuppress_DifferentFrequencyInsideWindow_ReturnsFalse()
    {
        var suppressor = new DuplicateCommandSuppressor(TimeSpan.FromMilliseconds(500));
        var now = DateTimeOffset.Parse("2026-07-16T12:00:00Z");

        Assert.IsFalse(suppressor.ShouldSuppress(14_250_000, now));
        Assert.IsFalse(suppressor.ShouldSuppress(7_074_000, now.AddMilliseconds(100)));
    }
}
