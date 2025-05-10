using Namezr.Features.Eligibility.Services;

namespace Namezr.Features.Eligibility.Helpers;

/// <summary>
/// See <see cref="EligibilityResult.SelectionWave"/>.
/// </summary>
internal class SelectionWaveComparer : IComparer<int?>
{
    public static readonly SelectionWaveComparer Instance = new();

    private SelectionWaveComparer()
    {
    }

    // TODO: unit test this
    public int Compare(int? x, int? y)
    {
        if (x is null)
        {
            return y is null ? 0 : +1;
        }

        return y is null ? -1 : x.Value.CompareTo(y);
    }
}