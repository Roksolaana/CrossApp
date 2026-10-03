using System.Globalization;

namespace Core.Dto;

public sealed record ImportResult<T>(IReadOnlyList<T> Items, IReadOnlyList<string> Errors)
{
    public int Total => Items.Count + Errors.Count;

    public double ErrorRate => Total == 0 ? 0 : Errors.Count * 100.0 / Total;

    public string ErrorRateFormatted => ErrorRate.ToString("F1", CultureInfo.InvariantCulture);
}