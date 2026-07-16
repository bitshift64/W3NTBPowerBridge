using W3NTBPowerBridge.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace W3NTBPowerBridge.Tests;

[TestClass]
public sealed class ShellyPowerServiceTests
{
    [TestMethod]
    public void ParseStatus_ShellyPlugStatus_ReturnsElectricalValues()
    {
        const string json = """
            {
              "id": 0,
              "source": "HTTP_in",
              "output": true,
              "apower": 47.8,
              "voltage": 121.4,
              "current": 0.39,
              "freq": 60.0
            }
            """;

        var status = ShellyPowerService.ParseStatus(json, 5.0);

        Assert.IsTrue(status.IsEnabled);
        Assert.IsTrue(status.IsReachable);
        Assert.AreEqual(true, status.OutputOn);
        Assert.AreEqual(47.8, status.PowerWatts);
        Assert.AreEqual(121.4, status.Voltage);
        Assert.AreEqual(0.39, status.CurrentAmps);
        Assert.AreEqual(60.0, status.FrequencyHz);
        Assert.IsFalse(status.StationOffConfirmed);
    }

    [TestMethod]
    public void ParseStatus_LowPowerOnRelay_ConfirmsStationOff()
    {
        const string json = """
            {
              "output": true,
              "apower": 1.2,
              "voltage": 121.0,
              "current": 0.01
            }
            """;

        var status = ShellyPowerService.ParseStatus(json, 5.0);

        Assert.IsTrue(status.StationOffConfirmed);
    }

    [TestMethod]
    public void ParseStatus_RelayOff_ConfirmsStationOff()
    {
        const string json = """
            {
              "output": false,
              "apower": 0,
              "voltage": 121.0,
              "current": 0
            }
            """;

        var status = ShellyPowerService.ParseStatus(json, 5.0);

        Assert.IsTrue(status.StationOffConfirmed);
        Assert.AreEqual(false, status.OutputOn);
    }
}
