using System.Globalization;

namespace W3NTBPowerBridge.Services;

/// <summary>
/// Converts frequency values between display and radio command units.
/// </summary>
public static class FrequencyConverter
{
    /// <summary>
    /// Attempts to convert a frequency in MHz to whole Hz.
    /// </summary>
    /// <param name="mhzText">Frequency text expressed in MHz.</param>
    /// <param name="frequencyHz">Converted integer Hz value.</param>
    /// <returns>True when conversion succeeds.</returns>
    public static bool TryMhzToHz(string mhzText, out long frequencyHz)
    {
        frequencyHz = 0;
        if (!decimal.TryParse(mhzText.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var mhz) || mhz <= 0)
        {
            return false;
        }

        frequencyHz = (long)Math.Round(mhz * 1_000_000m, MidpointRounding.AwayFromZero);
        return true;
    }

    /// <summary>
    /// Formats a Hz value as MHz for the user interface.
    /// </summary>
    /// <param name="frequencyHz">Frequency in Hz.</param>
    /// <returns>A MHz display string.</returns>
    public static string FormatHzAsMhz(long? frequencyHz)
    {
        return frequencyHz.HasValue ? $"{frequencyHz.Value / 1_000_000.0:0.000000} MHz" : "Not available";
    }

    /// <summary>
    /// Formats a Hz value as ACLog's MHz text value.
    /// </summary>
    /// <param name="frequencyHz">Frequency in Hz.</param>
    /// <returns>Frequency in MHz with fixed precision.</returns>
    public static string FormatHzAsAcLogMhz(long frequencyHz)
    {
        return $"{frequencyHz / 1_000_000.0:0.000000}";
    }

    /// <summary>
    /// Converts a frequency in Hz to an amateur band label used by ACLog.
    /// </summary>
    /// <param name="frequencyHz">Frequency in Hz.</param>
    /// <returns>The band label, or an empty string when the band is unknown.</returns>
    public static string HzToBand(long frequencyHz)
    {
        return frequencyHz switch
        {
            >= 1_800_000 and <= 2_000_000 => "160",
            >= 3_500_000 and <= 4_000_000 => "80",
            >= 5_330_000 and <= 5_407_000 => "60",
            >= 7_000_000 and <= 7_300_000 => "40",
            >= 10_100_000 and <= 10_150_000 => "30",
            >= 14_000_000 and <= 14_350_000 => "20",
            >= 18_068_000 and <= 18_168_000 => "17",
            >= 21_000_000 and <= 21_450_000 => "15",
            >= 24_890_000 and <= 24_990_000 => "12",
            >= 28_000_000 and <= 29_700_000 => "10",
            >= 50_000_000 and <= 54_000_000 => "6",
            >= 144_000_000 and <= 148_000_000 => "2",
            >= 222_000_000 and <= 225_000_000 => "1.25",
            >= 420_000_000 and <= 450_000_000 => "70CM",
            _ => string.Empty
        };
    }
}
