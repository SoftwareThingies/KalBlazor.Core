using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace SoftwareThingies.KalBlazor.Core.DataGrid;

/// <summary>
/// Hosts the hidden child row content used to detect filter matches in descendants of a row.
/// Re-rendering every probe whenever the grid re-renders is expensive for large data sets, so a probe only
/// renders again when its item or the filter state changes.
/// </summary>
internal sealed class KalDataGridDescendantFilterProbe : ComponentBase
{
    private bool _hasRendered;
    private bool _shouldRender = true;
    private object? _lastItem;
    private string? _lastFilterText;
    private bool _lastHasExactFilterMatch;
    private int _lastVersion;

    [Parameter]
    public object? Item { get; set; }

    [Parameter]
    public string? FilterText { get; set; }

    [Parameter]
    public bool HasExactFilterMatch { get; set; }

    /// <summary>
    /// Incremented by the grid to force all probes to render again, e.g. on <see cref="KalDataGrid{TItem}.RefreshAsync" />.
    /// </summary>
    [Parameter]
    public int Version { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override void OnParametersSet()
    {
        _shouldRender = !_hasRendered
            || !Equals(_lastItem, Item)
            || !string.Equals(_lastFilterText, FilterText, StringComparison.Ordinal)
            || _lastHasExactFilterMatch != HasExactFilterMatch
            || _lastVersion != Version;

        _lastItem = Item;
        _lastFilterText = FilterText;
        _lastHasExactFilterMatch = HasExactFilterMatch;
        _lastVersion = Version;
    }

    protected override bool ShouldRender() => _shouldRender;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        _hasRendered = true;
        builder.AddContent(0, ChildContent);
    }
}

/// <summary>
/// Groups all descendant filter probes of a grid. Passing parameters to thousands of probes is noticeable on every
/// grid render, so the probes are only revisited when the grid's filter state changed.
/// </summary>
internal sealed class KalDataGridDescendantFilterProbeHost : ComponentBase
{
    private bool _hasRendered;
    private bool _shouldRender = true;
    private int _lastVersion;

    /// <summary>
    /// Changes whenever the items, the filter or anything else affecting the probes changed.
    /// </summary>
    [Parameter]
    public int Version { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override void OnParametersSet()
    {
        _shouldRender = !_hasRendered || _lastVersion != Version;
        _lastVersion = Version;
    }

    protected override bool ShouldRender() => _shouldRender;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        _hasRendered = true;
        builder.AddContent(0, ChildContent);
    }
}
