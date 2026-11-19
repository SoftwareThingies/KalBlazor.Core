using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace SoftwareThingies.KalBlazor.Core.DataGrid;

/// <summary>
/// Highlights matching text inside a custom DataGrid cell template while preserving its formatting.
/// </summary>
public sealed class KalDataGridHighlight : ComponentBase, IDisposable
{
    private string? _lastFilterText;
    private bool _lastHasMatch;
    private bool _lastParentFilterOverride;
    private bool _hasReportedState;
    private IKalDataGridFilterContext? _subscribedFilterContext;

    /// <summary>
    /// The displayed text to highlight when it matches the grid filter.
    /// </summary>
    [Parameter]
    public string? Text { get; set; }

    [CascadingParameter]
    private IKalDataGridFilterContext? FilterContext { get; set; }

    [CascadingParameter(Name = KalDataGridDescendantContext.CascadeName)]
    private KalDataGridDescendantContext? DescendantContext { get; set; }

    private bool ParentFilterOverride => DescendantContext?.OverrideFilter ?? false;

    private KalDataGridDescendantFilterMatchContext? DescendantFilterMatchContext => DescendantContext?.MatchContext;

    protected override void OnParametersSet()
    {
        if (!ReferenceEquals(_subscribedFilterContext, FilterContext))
        {
            if (_subscribedFilterContext is not null)
            {
                _subscribedFilterContext.FilterStateChanged -= OnFilterStateChanged;
            }

            _subscribedFilterContext = FilterContext;

            if (_subscribedFilterContext is not null)
            {
                _subscribedFilterContext.FilterStateChanged += OnFilterStateChanged;
            }
        }

        UpdateMatchState();
    }

    private void OnFilterStateChanged()
    {
        UpdateMatchState();
        StateHasChanged();
    }

    private void UpdateMatchState()
    {
        var filterText = FilterContext?.FilterText;
        var hasMatch = MatchesFilter(Text, filterText);

        if (DescendantFilterMatchContext is not null
            && (!_hasReportedState
                || !string.Equals(_lastFilterText, filterText, StringComparison.Ordinal)
                || _lastHasMatch != hasMatch
                || _lastParentFilterOverride != ParentFilterOverride))
        {
            DescendantFilterMatchContext.ReportMatch(this, hasMatch);
        }

        _lastFilterText = filterText;
        _lastHasMatch = hasMatch;
        _lastParentFilterOverride = ParentFilterOverride;
        _hasReportedState = true;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var text = Text ?? string.Empty;
        var ranges = KalDataGridSearch.GetHighlightRanges(text, FilterContext?.FilterText);

        if (ranges.Count == 0)
        {
            builder.AddContent(0, text);
            return;
        }

        var sequence = 0;
        var position = 0;

        foreach (var (start, end) in ranges)
        {
            if (start > position)
            {
                builder.AddContent(sequence++, text[position..start]);
            }

            builder.OpenElement(sequence++, "span");
            builder.AddAttribute(sequence++, "class", FilterContext?.HighlightClass);
            builder.AddContent(sequence++, text[start..end]);
            builder.CloseElement();
            position = end;
        }

        if (position < text.Length)
        {
            builder.AddContent(sequence++, text[position..]);
        }
    }

    public void Dispose()
    {
        if (_subscribedFilterContext is not null)
        {
            _subscribedFilterContext.FilterStateChanged -= OnFilterStateChanged;
        }
    }

    private static bool MatchesFilter(string? text, string? filterText)
    {
        return !string.IsNullOrWhiteSpace(text)
            && !string.IsNullOrWhiteSpace(filterText)
            && filterText
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(term => KalDataGridSearch.Contains(text, term));
    }
}
