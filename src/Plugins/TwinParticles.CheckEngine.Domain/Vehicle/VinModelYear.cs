using System;

namespace TwinParticles.CheckEngine.Domain.Vehicle;

public static class VinModelYear
{
    public static bool TryDecode(char yearCode, out int modelYear)
    {
        modelYear = yearCode switch
        {
            'A' => 2010,
            'B' => 2011,
            'C' => 2012,
            'D' => 2013,
            'E' => 2014,
            'F' => 2015,
            'G' => 2016,
            'H' => 2017,
            'J' => 2018,
            'K' => 2019,
            'L' => 2020,
            'M' => 2021,
            'N' => 2022,
            'P' => 2023,
            'R' => 2024,
            'S' => 2025,
            'T' => 2026,
            'V' => 2027,
            'W' => 2028,
            'X' => 2029,
            'Y' => 2030,
            '1' => 2001,
            '2' => 2002,
            '3' => 2003,
            '4' => 2004,
            '5' => 2005,
            '6' => 2006,
            '7' => 2007,
            '8' => 2008,
            '9' => 2009,
            _ => 0
        };

        return modelYear > 0;
    }

    public static int? DecodeFromVin(string normalizedVin)
    {
        if (string.IsNullOrWhiteSpace(normalizedVin) || normalizedVin.Length != 17)
            return null;

        return TryDecode(normalizedVin[9], out var year) ? year : null;
    }
}
