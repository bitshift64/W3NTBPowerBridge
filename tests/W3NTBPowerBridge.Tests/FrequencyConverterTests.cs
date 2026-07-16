using W3NTBPowerBridge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace W3NTBPowerBridge.Tests;

[TestClass]
public sealed class FrequencyConverterTests
{
    [DataTestMethod]
    [DataRow("14.250", 14250000L)]
    [DataRow("7.074", 7074000L)]
    [DataRow("1.234567", 1234567L)]
    public void TryMhzToHz_ValidMhz_ConvertsToIntegerHz(string mhz, long expectedHz)
    {
        var converted = FrequencyConverter.TryMhzToHz(mhz, out var actualHz);

        Assert.IsTrue(converted);
        Assert.AreEqual(expectedHz, actualHz);
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("not a number")]
    [DataRow("-14.250")]
    public void TryMhzToHz_InvalidInput_ReturnsFalse(string mhz)
    {
        var converted = FrequencyConverter.TryMhzToHz(mhz, out _);

        Assert.IsFalse(converted);
    }

    [DataTestMethod]
    [DataRow(14_250_000L, "20")]
    [DataRow(7_074_000L, "40")]
    [DataRow(50_313_000L, "6")]
    public void HzToBand_AmateurFrequency_ReturnsAcLogBand(long frequencyHz, string expectedBand)
    {
        Assert.AreEqual(expectedBand, FrequencyConverter.HzToBand(frequencyHz));
    }
}
