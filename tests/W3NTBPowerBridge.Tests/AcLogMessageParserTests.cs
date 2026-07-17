using W3NTBPowerBridge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace W3NTBPowerBridge.Tests;

[TestClass]
public sealed class AcLogMessageParserTests
{
    [TestMethod]
    public void TryParseChangeFrequency_ValidXml_ReturnsFrequencyRequest()
    {
        const string xml = "<CMD><CHANGEFREQ><VALUE>14.250</VALUE></CHANGEFREQ></CMD>";

        var parsed = AcLogMessageParser.TryParseChangeFrequency(xml, out var request);

        Assert.IsTrue(parsed);
        Assert.AreEqual(14_250_000, request.FrequencyHz);
        Assert.AreEqual("14.250", request.OriginalValue);
    }

    [TestMethod]
    public void TryParseChangeFrequency_LegacyCommandElement_ReturnsFrequencyRequest()
    {
        const string xml = "<CMD><CHANGEFREQ><VALUE>7.074</VALUE></CHANGEFREQ></CMD>";

        var parsed = AcLogMessageParser.TryParseChangeFrequency(xml, out var request);

        Assert.IsTrue(parsed);
        Assert.AreEqual(7_074_000, request.FrequencyHz);
    }

    [TestMethod]
    public void TryParseChangeFrequency_MalformedXml_ReturnsFalse()
    {
        var parsed = AcLogMessageParser.TryParseChangeFrequency("<CMD><COMMAND>CHANGEFREQ", out _);

        Assert.IsFalse(parsed);
    }

    [TestMethod]
    public void TryParseChangeFrequency_OtherCommand_ReturnsFalse()
    {
        const string xml = "<CMD><COMMAND>CONTACT</COMMAND><VALUE>14.250</VALUE></CMD>";

        var parsed = AcLogMessageParser.TryParseChangeFrequency(xml, out _);

        Assert.IsFalse(parsed);
    }

    [TestMethod]
    public void TryParseFrequencyUpdate_DxClusterFrequencyField_ReturnsFrequencyRequest()
    {
        const string xml = "<CMD><UPDATERESPONSE><CONTROL>TXTENTRYFREQUENCY</CONTROL><VALUE>14.24500</VALUE></CMD>";

        var parsed = AcLogMessageParser.TryParseFrequencyUpdate(xml, out var request);

        Assert.IsTrue(parsed);
        Assert.AreEqual(14_245_000, request.FrequencyHz);
        Assert.AreEqual("TXTENTRYFREQUENCY", request.Source);
    }

    [TestMethod]
    public void TryParseModeUpdate_DxClusterModeField_ReturnsMode()
    {
        const string xml = "<CMD><UPDATERESPONSE><CONTROL>TXTENTRYMODE</CONTROL><VALUE>SSB</VALUE></CMD>";

        var parsed = AcLogMessageParser.TryParseModeUpdate(xml, out var mode);

        Assert.IsTrue(parsed);
        Assert.AreEqual("SSB", mode);
    }

    [TestMethod]
    public void TryParseReadBmfResponse_ValidXml_ReturnsFrequencyAndMode()
    {
        const string xml = "<CMD><READBMFRESPONSE><BAND>40</BAND><MODE>SSB</MODE><MODETEST>PH</MODETEST><FREQ>7.22802</FREQ></READBMFRESPONSE></CMD>";

        var parsed = AcLogMessageParser.TryParseReadBmfResponse(xml, out var request);

        Assert.IsTrue(parsed);
        Assert.AreEqual(7_228_020, request.FrequencyHz);
        Assert.AreEqual("7.22802", request.OriginalValue);
        Assert.AreEqual("SSB", request.Mode);
        Assert.AreEqual("READBMFRESPONSE", request.Source);
    }

    [TestMethod]
    public void TryParseReadBmfResponse_UsesModeTestWhenModeIsBlank()
    {
        const string xml = "<CMD><READBMFRESPONSE><BAND>20</BAND><MODE></MODE><MODETEST>DIG</MODETEST><FREQ>14.074</FREQ></READBMFRESPONSE></CMD>";

        var parsed = AcLogMessageParser.TryParseReadBmfResponse(xml, out var request);

        Assert.IsTrue(parsed);
        Assert.AreEqual(14_074_000, request.FrequencyHz);
        Assert.AreEqual("DIG", request.Mode);
    }

    [TestMethod]
    public void TryParseReadBmfResponse_MissingFrequency_ReturnsFalse()
    {
        const string xml = "<CMD><READBMFRESPONSE><BAND>40</BAND><MODE>SSB</MODE></READBMFRESPONSE></CMD>";

        var parsed = AcLogMessageParser.TryParseReadBmfResponse(xml, out _);

        Assert.IsFalse(parsed);
    }

    [TestMethod]
    public void TryParseReadBmfResponse_MalformedXml_ReturnsFalse()
    {
        var parsed = AcLogMessageParser.TryParseReadBmfResponse("<CMD><READBMFRESPONSE><FREQ>7.22802", out _);

        Assert.IsFalse(parsed);
    }
}
