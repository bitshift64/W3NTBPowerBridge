namespace W3NTBPowerBridge.Services;

/// <summary>
/// Parses wfview Hamlib rigctld responses.
/// </summary>
public static class HamlibResponseParser
{
    /// <summary>
    /// Attempts to parse a rigctld frequency response.
    /// </summary>
    /// <param name="response">Raw response text from rigctld.</param>
    /// <param name="frequencyHz">Parsed frequency in Hz.</param>
    /// <returns>True when the response contains a valid integer frequency.</returns>
    public static bool TryParseFrequency(string response, out long frequencyHz)
    {
        frequencyHz = 0;
        var lines = response.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var line in lines)
        {
            if (line.StartsWith("RPRT", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (TryParseFrequencyLine(line, out frequencyHz))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryParseFrequencyLine(string line, out long frequencyHz)
    {
        frequencyHz = 0;
        if (long.TryParse(line, out frequencyHz) && frequencyHz > 0)
        {
            return true;
        }

        if (decimal.TryParse(line, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var decimalHz) && decimalHz > 0)
        {
            frequencyHz = (long)Math.Round(decimalHz, MidpointRounding.AwayFromZero);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to parse a rigctld mode response.
    /// </summary>
    /// <param name="response">Raw response text from rigctld.</param>
    /// <param name="mode">Parsed mode value.</param>
    /// <returns>True when a mode value is found.</returns>
    public static bool TryParseMode(string response, out string mode)
    {
        mode = string.Empty;
        var lines = response.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var line in lines)
        {
            if (line.StartsWith("RPRT", StringComparison.OrdinalIgnoreCase) || long.TryParse(line, out _))
            {
                continue;
            }

            mode = line.ToUpperInvariant();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to parse a rigctld level response such as RFPOWER.
    /// </summary>
    /// <param name="response">Raw response text from rigctld.</param>
    /// <param name="level">Parsed level value.</param>
    /// <returns>True when a level value is found.</returns>
    public static bool TryParseLevel(string response, out double level)
    {
        level = 0;
        var lines = response.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var line in lines)
        {
            if (line.StartsWith("RPRT", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (double.TryParse(line, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out level))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Attempts to parse a rigctld PTT response.
    /// </summary>
    /// <param name="response">Raw response text from rigctld.</param>
    /// <param name="isTransmitting">True when PTT/transmit is active.</param>
    /// <returns>True when a PTT value is found.</returns>
    public static bool TryParsePtt(string response, out bool isTransmitting)
    {
        isTransmitting = false;
        var lines = response.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var line in lines)
        {
            if (line.StartsWith("RPRT", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (int.TryParse(line, out var pttValue))
            {
                isTransmitting = pttValue > 0;
                return true;
            }

            if (bool.TryParse(line, out isTransmitting))
            {
                return true;
            }
        }

        return false;
    }
}
