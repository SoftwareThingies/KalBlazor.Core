namespace SoftwareThingies.KalBlazor.Core.DataGrid;

/// <summary>
/// Cascaded by a grid into the content of a child row. A single cascaded value keeps the number of components per
/// child row low, which matters for the thousands of hidden filter probes of a large, filtered grid.
/// </summary>
/// <param name="OverrideFilter">The parent row matches the filter, so descendants show all of their items.</param>
/// <param name="MatchContext">Receives whether the descendants contain filter matches.</param>
/// <param name="IsFilterProbe">The content is rendered hidden only to detect filter matches.</param>
internal sealed record KalDataGridDescendantContext(
    bool OverrideFilter,
    KalDataGridDescendantFilterMatchContext MatchContext,
    bool IsFilterProbe)
{
    public const string CascadeName = "KalDataGridDescendant";
}
