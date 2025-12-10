public static class TimeZoneNumericMap
{
    // Windows timezone Id -> numeric code expected by the other system
    private static readonly IReadOnlyDictionary<string, int> WindowsToNumeric =
       new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
       {
        { "Dateline Standard Time", 0 },
        { "Samoa Standard Time", 1 },
        { "Hawaiian Standard Time", 2 },
        { "Alaskan Standard Time", 3 },
        { "Pacific Standard Time", 4 },

        { "Mountain Standard Time", 10 },
        { "Mexico Standard Time 2", 13 },
        { "U.S. Mountain Standard Time", 15 },

        { "Central Standard Time", 20 },
        { "Canada Central Standard Time", 25 },
        { "Mexico Standard Time", 30 },
        { "Central America Standard Time", 33 },

        { "Eastern Standard Time", 35 },
        { "U.S. Eastern Standard Time", 40 },
        { "S.A. Pacific Standard Time", 45 },

        { "Atlantic Standard Time", 50 },
        { "S.A. Western Standard Time", 55 },
        { "Pacific S.A. Standard Time", 56 },

        { "Newfoundland and Labrador Standard Time", 60 },

        { "E. South America Standard Time", 65 },
        { "S.A. Eastern Standard Time", 70 },
        { "Greenland Standard Time", 73 },

        { "Mid-Atlantic Standard Time", 75 },
        { "Azores Standard Time", 80 },
        { "Cape Verde Standard Time", 83 },

        { "GMT Standard Time", 85 },
        { "Greenwich Standard Time", 90 },

        { "Central Europe Standard Time", 95 },
        { "Central European Standard Time", 100 },
        { "Romance Standard Time", 105 },
        { "W. Europe Standard Time", 110 },
        { "W. Central Africa Standard Time", 113 },

        { "E. Europe Standard Time", 115 },
        { "Egypt Standard Time", 120 },
        { "FLE Standard Time", 125 },
        { "GTB Standard Time", 130 },

        { "Israel Standard Time", 135 },
        { "South Africa Standard Time", 140 },
        { "Russian Standard Time", 145 },
        { "Arab Standard Time", 150 },
        { "E. Africa Standard Time", 155 },
        { "Arabic Standard Time", 158 },

        { "Iran Standard Time", 160 },
        { "Arabian Standard Time", 165 },
        { "Caucasus Standard Time", 170 },
        { "Transitional Islamic State of Afghanistan Standard Time", 175 },

        { "Ekaterinburg Standard Time", 180 },
        { "West Asia Standard Time", 185 },
        { "India Standard Time", 190 },
        { "Nepal Standard Time", 193 },
        { "Central Asia Standard Time", 195 },

        { "Sri Lanka Standard Time", 200 },
        { "N. Central Asia Standard Time", 201 },
        { "Myanmar Standard Time", 203 },
        { "S.E. Asia Standard Time", 205 },
        { "North Asia Standard Time", 207 },

        { "China Standard Time", 210 },
        { "Singapore Standard Time", 215 },
        { "Taipei Standard Time", 220 },
        { "W. Australia Standard Time", 225 },
        { "North Asia East Standard Time", 227 },

        { "Korea Standard Time", 230 },
        { "Tokyo Standard Time", 235 },
        { "Yakutsk Standard Time", 240 },

        { "A.U.S. Central Standard Time", 245 },
        { "Cen. Australia Standard Time", 250 },
        { "A.U.S. Eastern Standard Time", 255 },
        { "E. Australia Standard Time", 260 },
        { "Tasmania Standard Time", 265 },

        { "Vladivostok Standard Time", 270 },
        { "West Pacific Standard Time", 275 },
        { "Central Pacific Standard Time", 280 },
        { "Fiji Islands Standard Time", 285 },
        { "New Zealand Standard Time", 290 },

        { "Tonga Standard Time", 300 },
       };

    public static bool TryGetNumericId(string timeZoneId, out int numericId)
    {
        // Normalize to Windows ID (in case we get IANA)
        var windowsId = TimeZoneConverter.TZConvert.TryIanaToWindows(timeZoneId, out var w)
            ? w
            : timeZoneId; // assume already Windows

        return WindowsToNumeric.TryGetValue(windowsId, out numericId);
    }
}
