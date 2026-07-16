using W3NTBPowerBridge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace W3NTBPowerBridge.Tests;

[TestClass]
public sealed class HamlibResponseParserTests
{
    [TestMethod]
    public void TryParseFrequency_IntegerLine_ReturnsFrequency()
    {
        var parsed = HamlibResponseParser.TryParseFrequency("14250000\n", out var frequencyHz);

        Assert.IsTrue(parsed);
        Assert.AreEqual(14_250_000, frequencyHz);
    }

    [TestMethod]
    public void TryParseFrequency_DecimalHzLine_ReturnsRoundedFrequency()
    {
        var parsed = HamlibResponseParser.TryParseFrequency("7209200.000000\n", out var frequencyHz);

        Assert.IsTrue(parsed);
        Assert.AreEqual(7_209_200, frequencyHz);
    }

    [TestMethod]
    public void TryParseFrequency_IgnoresReportLine_ReturnsFrequency()
    {
        var parsed = HamlibResponseParser.TryParseFrequency("RPRT 0\n7074000\n", out var frequencyHz);

        Assert.IsTrue(parsed);
        Assert.AreEqual(7_074_000, frequencyHz);
    }

    [TestMethod]
    public void TryParseFrequency_NoFrequency_ReturnsFalse()
    {
        var parsed = HamlibResponseParser.TryParseFrequency("RPRT -1\n", out _);

        Assert.IsFalse(parsed);
    }

    [TestMethod]
    public void TryParseMode_ModeAndPassband_ReturnsMode()
    {
        var parsed = HamlibResponseParser.TryParseMode("USB\n2400\n", out var mode);

        Assert.IsTrue(parsed);
        Assert.AreEqual("USB", mode);
    }

    [TestMethod]
    public void TryParseLevel_RfPowerDecimal_ReturnsLevel()
    {
        var parsed = HamlibResponseParser.TryParseLevel("0.55\n", out var level);

        Assert.IsTrue(parsed);
        Assert.AreEqual(0.55, level, 0.001);
    }

    [TestMethod]
    public void TryParsePtt_Zero_ReturnsReceive()
    {
        var parsed = HamlibResponseParser.TryParsePtt("0\n", out var isTransmitting);

        Assert.IsTrue(parsed);
        Assert.IsFalse(isTransmitting);
    }

    [TestMethod]
    public void TryParsePtt_One_ReturnsTransmit()
    {
        var parsed = HamlibResponseParser.TryParsePtt("1\n", out var isTransmitting);

        Assert.IsTrue(parsed);
        Assert.IsTrue(isTransmitting);
    }

    [TestMethod]
    public void TryParsePtt_ReportOnly_ReturnsFalse()
    {
        var parsed = HamlibResponseParser.TryParsePtt("RPRT -1\n", out _);

        Assert.IsFalse(parsed);
    }
}
