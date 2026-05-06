using System;
using System.Collections.Generic;

namespace Audyssey;

/// <summary>
/// Maps Audyssey <c>commandId</c> tokens (FL, C, SW1, TFR, ...) onto
/// human-friendly speaker names. Falls back to the raw token when unknown.
/// </summary>
public static class AudysseyChannelNames
{
    private static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        { "FL",  "Front L" },
        { "FR",  "Front R" },
        { "C",   "Center" },
        { "SLA", "Surround L" },
        { "SRA", "Surround R" },
        { "SL",  "Surround L" },
        { "SR",  "Surround R" },
        { "SBL", "Surround Back L" },
        { "SBR", "Surround Back R" },
        { "TFL", "Top Front L" },
        { "TFR", "Top Front R" },
        { "TML", "Top Middle L" },
        { "TMR", "Top Middle R" },
        { "TRL", "Top Rear L" },
        { "TRR", "Top Rear R" },
        { "FHL", "Front Height L" },
        { "FHR", "Front Height R" },
        { "RHL", "Rear Height L" },
        { "RHR", "Rear Height R" },
        { "FWL", "Front Wide L" },
        { "FWR", "Front Wide R" },
        { "FDL", "Front Dolby L" },
        { "FDR", "Front Dolby R" },
        { "SDL", "Surround Dolby L" },
        { "SDR", "Surround Dolby R" },
        { "BDL", "Back Dolby L" },
        { "BDR", "Back Dolby R" },
    };

    public static string Friendly(string commandId)
    {
        if (string.IsNullOrEmpty(commandId)) return string.Empty;
        if (commandId.StartsWith("SW", StringComparison.OrdinalIgnoreCase))
        {
            return commandId.Length > 2 ? string.Concat("Subwoofer ", commandId.AsSpan(2)) : "Subwoofer";
        }
        return Map.TryGetValue(commandId, out string name) ? name : commandId;
    }
}
