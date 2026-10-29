namespace SoftwareThingies.KalBlazor.Core.DataGrid;

internal sealed class KalDataGridDescendantFilterMatchContext(Action<bool> reportMatch)
{
    private readonly HashSet<object> _matchingReporters = [];

    public void ReportMatch(object reporter, bool hasMatch)
    {
        var hadMatch = _matchingReporters.Count > 0;

        if (hasMatch)
        {
            _matchingReporters.Add(reporter);
        }
        else
        {
            _matchingReporters.Remove(reporter);
        }

        var hasAnyMatch = _matchingReporters.Count > 0;
        if (hadMatch != hasAnyMatch)
        {
            reportMatch(hasAnyMatch);
        }
    }
}

