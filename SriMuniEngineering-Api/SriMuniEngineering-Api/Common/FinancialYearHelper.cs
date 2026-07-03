namespace SriMuniEngineering_Api.Common;

/// <summary>
/// Helper for Indian Financial Year calculations.
/// Indian FY runs from 1 April to 31 March.
/// </summary>
public static class FinancialYearHelper
{
    /// <summary>
    /// Returns the financial year string for a given date.
    /// Example: 01-Apr-2026 to 31-Mar-2027 → "26-27"
    ///          01-Apr-2027 to 31-Mar-2028 → "27-28"
    /// </summary>
    public static string GetFinancialYear(DateTime date)
    {
        int startYear;

        if (date.Month >= 4) // April onwards = same year is the FY start
        {
            startYear = date.Year;
        }
        else // Jan-Mar = previous year is the FY start
        {
            startYear = date.Year - 1;
        }

        var endYear = startYear + 1;

        return $"{startYear % 100:D2}-{endYear % 100:D2}";
    }

    /// <summary>
    /// Returns the financial year string for the current UTC date.
    /// </summary>
    public static string GetCurrentFinancialYear()
    {
        return GetFinancialYear(DateTime.UtcNow);
    }
}
