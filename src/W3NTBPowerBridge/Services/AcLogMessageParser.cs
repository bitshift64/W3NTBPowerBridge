using System.Xml.Linq;
using W3NTBPowerBridge.Models;

namespace W3NTBPowerBridge.Services;

/// <summary>
/// Parses ACLog TCP API XML messages.
/// </summary>
public static class AcLogMessageParser
{
    /// <summary>
    /// Attempts to parse a CHANGEFREQ request from an ACLog XML message.
    /// </summary>
    /// <param name="xml">The incoming XML message.</param>
    /// <param name="request">The parsed frequency request when parsing succeeds.</param>
    /// <returns>True when the message is a valid CHANGEFREQ request.</returns>
    public static bool TryParseChangeFrequency(string xml, out FrequencyRequest request)
    {
        request = new FrequencyRequest(0, string.Empty);

        try
        {
            var document = XDocument.Parse(xml, LoadOptions.None);
            var changeFrequencyElement = FindChangeFrequencyElement(document.Root);
            if (changeFrequencyElement is null)
            {
                return false;
            }

            var value = FindValue(changeFrequencyElement, "VALUE");
            if (string.IsNullOrWhiteSpace(value) || !FrequencyConverter.TryMhzToHz(value, out var frequencyHz))
            {
                return false;
            }

            request = new FrequencyRequest(frequencyHz, value.Trim());
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Attempts to parse an ACLog text field update message.
    /// </summary>
    /// <param name="xml">The incoming XML message.</param>
    /// <param name="control">The updated ACLog control name.</param>
    /// <param name="value">The updated control value.</param>
    /// <returns>True when the message is an UPDATERESPONSE.</returns>
    public static bool TryParseUpdateResponse(string xml, out string control, out string value)
    {
        control = string.Empty;
        value = string.Empty;

        try
        {
            var document = XDocument.Parse(xml, LoadOptions.None);
            var updateElement = document.Root?.DescendantsAndSelf()
                .FirstOrDefault(element => string.Equals(element.Name.LocalName, "UPDATERESPONSE", StringComparison.OrdinalIgnoreCase));
            if (updateElement is null)
            {
                return false;
            }

            control = FindValue(updateElement, "CONTROL") ?? string.Empty;
            value = FindValue(updateElement, "VALUE") ?? string.Empty;
            return !string.IsNullOrWhiteSpace(control);
        }
        catch
        {
            if (!xml.Contains("<UPDATERESPONSE>", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            control = ExtractTagValue(xml, "CONTROL") ?? string.Empty;
            value = ExtractTagValue(xml, "VALUE") ?? string.Empty;
            return !string.IsNullOrWhiteSpace(control);
        }
    }

    /// <summary>
    /// Attempts to parse a frequency text field update from ACLog.
    /// </summary>
    /// <param name="xml">The incoming XML message.</param>
    /// <param name="request">The parsed frequency request.</param>
    /// <returns>True when the update contains a frequency value.</returns>
    public static bool TryParseFrequencyUpdate(string xml, out FrequencyRequest request)
    {
        request = new FrequencyRequest(0, string.Empty);
        if (!TryParseUpdateResponse(xml, out var control, out var value)
            || !string.Equals(control, "TXTENTRYFREQUENCY", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(value)
            || !FrequencyConverter.TryMhzToHz(value, out var frequencyHz))
        {
            return false;
        }

        request = new FrequencyRequest(frequencyHz, value.Trim(), null, "TXTENTRYFREQUENCY");
        return true;
    }

    /// <summary>
    /// Attempts to parse ACLog's band, mode, and frequency response.
    /// </summary>
    /// <param name="xml">The incoming XML message.</param>
    /// <param name="request">The parsed frequency request.</param>
    /// <returns>True when the response contains a valid frequency value.</returns>
    public static bool TryParseReadBmfResponse(string xml, out FrequencyRequest request)
    {
        request = new FrequencyRequest(0, string.Empty);

        try
        {
            var document = XDocument.Parse(xml, LoadOptions.None);
            var responseElement = document.Root?.DescendantsAndSelf()
                .FirstOrDefault(element => string.Equals(element.Name.LocalName, "READBMFRESPONSE", StringComparison.OrdinalIgnoreCase));
            if (responseElement is null)
            {
                return false;
            }

            var frequencyMhz = FindValue(responseElement, "FREQ");
            if (string.IsNullOrWhiteSpace(frequencyMhz) || !FrequencyConverter.TryMhzToHz(frequencyMhz, out var frequencyHz))
            {
                return false;
            }

            var mode = FindValue(responseElement, "MODE");
            if (string.IsNullOrWhiteSpace(mode))
            {
                mode = FindValue(responseElement, "MODETEST");
            }

            request = new FrequencyRequest(frequencyHz, frequencyMhz.Trim(), NormalizeOptionalMode(mode), "READBMFRESPONSE");
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Attempts to parse a mode text field update from ACLog.
    /// </summary>
    /// <param name="xml">The incoming XML message.</param>
    /// <param name="mode">The parsed mode value.</param>
    /// <returns>True when the update contains a non-empty mode value.</returns>
    public static bool TryParseModeUpdate(string xml, out string mode)
    {
        mode = string.Empty;
        if (!TryParseUpdateResponse(xml, out var control, out var value)
            || !string.Equals(control, "TXTENTRYMODE", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        mode = value.Trim().ToUpperInvariant();
        return true;
    }

    private static string? FindValue(XElement? root, string name)
    {
        return root?.DescendantsAndSelf()
            .FirstOrDefault(element => string.Equals(element.Name.LocalName, name, StringComparison.OrdinalIgnoreCase))
            ?.Value
            ?.Trim();
    }

    private static XElement? FindChangeFrequencyElement(XElement? root)
    {
        if (root is null)
        {
            return null;
        }

        return root.DescendantsAndSelf()
            .FirstOrDefault(element => string.Equals(element.Name.LocalName, "CHANGEFREQ", StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizeOptionalMode(string? mode)
    {
        return string.IsNullOrWhiteSpace(mode) ? null : mode.Trim().ToUpperInvariant();
    }

    private static string? ExtractTagValue(string xml, string tagName)
    {
        var openTag = $"<{tagName}>";
        var closeTag = $"</{tagName}>";
        var start = xml.IndexOf(openTag, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
        {
            return null;
        }

        start += openTag.Length;
        var end = xml.IndexOf(closeTag, start, StringComparison.OrdinalIgnoreCase);
        if (end < 0)
        {
            return null;
        }

        return xml[start..end].Trim();
    }
}
