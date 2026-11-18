using Microsoft.AspNetCore.Components;

namespace SoftwareThingies.KalBlazor.Core.DataGrid;

public partial class KalDataGrid<TItem> : IDisposable, IKalDataGridFilterContext
{
    private const string DefaultTableClass = "w-full min-w-full";
    private const string DefaultHeaderClass = "";
    private const string DefaultHeaderRowClass = "";
    private const string DefaultBodyClass = "divide-y";
    private const string DefaultRowClass = "transition-colors";
    private const string DefaultExpandHeaderCellClass = "w-0";
    private const string DefaultExpandAllButtonClass = "inline-flex h-7 w-7 items-center justify-center rounded-md text-slate-500 transition-colors hover:bg-slate-100 hover:text-slate-900 focus:outline-none focus:ring-2 focus:ring-amber-200";
    private const string DefaultExpandCellClass = "w-0 align-top";
    private const string DefaultExpandButtonClass = "inline-flex h-7 w-7 items-center justify-center rounded-md text-slate-500 transition-colors hover:bg-slate-100 hover:text-slate-900 focus:outline-none focus:ring-2 focus:ring-amber-200";
    private const string DefaultChildRowClass = "kal-data-grid-child-row";
    private const string DefaultChildCellClass = "bg-slate-50";
    private const string DefaultChildContentClass = "w-full";
    private const string DefaultHighlightCss = "bg-yellow-200 shadow-sm shadow-gray-400";
    private static readonly object NullItemKey = new();
    protected override string ComponentClass => "kal-data-grid";

    protected override string DefaultClass => "w-full";

    private readonly HashSet<object?> _expandedItemKeys = [];
    private readonly HashSet<TItem> _descendantFilterMatches = [];
    private readonly Dictionary<object, KalDataGridDescendantFilterMatchContext> _descendantFilterMatchContexts = [];
    private readonly List<KalGridColumn<TItem>> _sortColumns = [];
    private readonly Dictionary<KalGridColumn<TItem>, KalDataGridSortDirection> _sortDirections = [];
    private bool _defaultExpansionApplied;
    private bool _hasRendered;
    private bool? _pendingDescendantExpansion;
    private KalDataGridChildRowExpansionContext? _subscribedParentChildRowExpansionContext;
    private int _lastHandledParentExpansionVersion;
    private string? _currentFilterText;
    private string? _lastFilterStateText;
    private string? _lastFilterTextParameter;
    private bool _lastParentFilterOverride;
    private bool? _lastReportedFilterMatch;
    private bool? _hasExactFilterMatchCache;
    private HashSet<TItem>? _filterMatchesCache;
    private TItem[]? _boundItemsCache;
    private TItem[] _itemsSnapshot = [];
    private int _refreshVersion;
    private int _descendantFilterProbeVersion;
    private (string? FilterText, bool? ParentHasExactFilterMatch, int ParentRefreshVersion)? _lastFilterCacheKey;
    private (string? FilterText, string HighlightClass, bool HasExactFilterMatch)? _lastNotifiedFilterState;
    private IKalDataGridFilterContext? _subscribedParentFilterContext;
    // Thousands of probes and highlights subscribe, so a set avoids the O(n) copy of a multicast delegate per change.
    private readonly HashSet<Action> _filterStateChangedHandlers = [];

    private KalDataGridContext<TItem> GridContext { get; } = new();

    private KalDataGridChildRowExpansionContext ChildRowExpansionContext { get; } = new();

    string? IKalDataGridFilterContext.FilterText => InheritedFilterText;

    string IKalDataGridFilterContext.HighlightClass => EffectiveHighlightClass;

    bool IKalDataGridFilterContext.HasExactFilterMatch => HasExactFilterMatch;

    int IKalDataGridFilterContext.RefreshVersion => RefreshVersion;

    /// <summary>
    /// Changes whenever this grid or one of its ancestors was refreshed, so nested grids re-evaluate as well.
    /// </summary>
    private int RefreshVersion => _refreshVersion + (ParentFilterContext?.RefreshVersion ?? 0);

    event Action? IKalDataGridFilterContext.FilterStateChanged
    {
        add
        {
            if (value is not null)
            {
                _filterStateChangedHandlers.Add(value);
            }
        }
        remove
        {
            if (value is not null)
            {
                _filterStateChangedHandlers.Remove(value);
            }
        }
    }

    [CascadingParameter]
    private KalDataGridChildRowExpansionContext? ParentChildRowExpansionContext { get; set; }

    [CascadingParameter]
    private IKalDataGridFilterContext? ParentFilterContext { get; set; }

    [CascadingParameter(Name = KalDataGridDescendantContext.CascadeName)]
    private KalDataGridDescendantContext? ParentDescendantContext { get; set; }

    private bool ParentFilterOverride => ParentDescendantContext?.OverrideFilter ?? false;

    private KalDataGridDescendantFilterMatchContext? ParentDescendantFilterMatchContext => ParentDescendantContext?.MatchContext;

    /// <summary>
    /// Set when this grid is rendered hidden inside a parent's descendant filter probe. Such a grid only has to
    /// report whether it contains matches, so it skips rendering its table.
    /// </summary>
    private bool IsDescendantFilterProbe => ParentDescendantContext?.IsFilterProbe ?? false;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The items shown by the grid. Filter and sort results are reused as long as the sequence of item instances is
    /// unchanged. Call <see cref="RefreshAsync" /> after changing properties of existing items in place.
    /// </summary>
    [Parameter]
    public IEnumerable<TItem>? Items { get; set; }

    [Parameter]
    public RenderFragment? Columns { get; set; }

    [Parameter]
    public RenderFragment<TItem>? ChildRowContent { get; set; }

    [Parameter]
    public RenderFragment? Filter { get; set; }

    [Parameter]
    public bool ShowFilter { get; set; }

    [Parameter]
    public string FilterPlaceholder { get; set; } = "Filter...";

    [Parameter]
    public string? FilterText { get; set; }

    [Parameter]
    public EventCallback<string?> FilterTextChanged { get; set; }

    /// <summary>
    /// Replaces the default classes used to highlight matching text in property cells.
    /// </summary>
    [Parameter]
    public string? HighlightClass { get; set; }

    [Parameter]
    public string? FilterClass { get; set; }

    [Parameter]
    public string? AdditionalFilterClass { get; set; }

    [Parameter]
    public string? FilterInputClass { get; set; }

    [Parameter]
    public string? AdditionalFilterInputClass { get; set; }

    /// <summary>
    /// Replaces the default table classes.
    /// </summary>
    [Parameter]
    public string? TableClass { get; set; }

    /// <summary>
    /// Additional classes appended to the table classes.
    /// </summary>
    [Parameter]
    public string? AdditionalTableClass { get; set; }

    [Parameter]
    public string? HeaderClass { get; set; }

    [Parameter]
    public string? AdditionalHeaderClass { get; set; }

    [Parameter]
    public string? HeaderRowClass { get; set; }

    [Parameter]
    public string? AdditionalHeaderRowClass { get; set; }

    [Parameter]
    public string? AdditionalHeaderCellClass { get; set; }

    [Parameter]
    public string SortIconAscending { get; set; } = Icons.ArrowUp;

    [Parameter]
    public string SortIconDescending { get; set; } = Icons.ArrowDown;

    [Parameter]
    public string? SortIconAscendingClass { get; set; }

    [Parameter]
    public string? SortIconDescendingClass { get; set; }

    [Parameter]
    public string? BodyClass { get; set; }

    [Parameter]
    public string? AdditionalBodyClass { get; set; }

    [Parameter]
    public string? RowClass { get; set; }

    [Parameter]
    public string? AdditionalRowClass { get; set; }

    [Parameter]
    public bool ExpandChildRowsByDefault { get; set; }

    /// <summary>
    /// Optional selector used to persist row UI state like expanded child rows across re-renders
    /// when the <see cref="Items" /> collection is recreated with new item instances.
    /// </summary>
    [Parameter]
    public Func<TItem, object?>? ItemKey { get; set; }

    [Parameter]
    public string ExpandHeaderText { get; set; } = "Details";

    [Parameter]
    public string ExpandButtonCollapsedAriaLabel { get; set; } = "Details anzeigen";

    [Parameter]
    public string ExpandButtonExpandedAriaLabel { get; set; } = "Details ausblenden";

    [Parameter]
    public string ExpandAllButtonCollapsedAriaLabel { get; set; } = "Alle Details anzeigen";

    [Parameter]
    public string ExpandAllButtonExpandedAriaLabel { get; set; } = "Alle Details ausblenden";

    [Parameter]
    public string? ExpandHeaderCellClass { get; set; }

    [Parameter]
    public string? AdditionalExpandHeaderCellClass { get; set; }

    [Parameter]
    public string? ExpandAllButtonClass { get; set; }

    [Parameter]
    public string? AdditionalExpandAllButtonClass { get; set; }

    [Parameter]
    public string? ExpandCellClass { get; set; }

    [Parameter]
    public string? AdditionalExpandCellClass { get; set; }

    [Parameter]
    public string? ExpandButtonClass { get; set; }

    [Parameter]
    public string? AdditionalExpandButtonClass { get; set; }

    [Parameter]
    public string? ChildRowClass { get; set; }

    [Parameter]
    public string? AdditionalChildRowClass { get; set; }

    [Parameter]
    public string? ChildCellClass { get; set; }

    [Parameter]
    public string? AdditionalChildCellClass { get; set; }

    [Parameter]
    public string? ChildContentClass { get; set; }

    [Parameter]
    public string? AdditionalChildContentClass { get; set; }

    private bool IsBound => Items is not null && Columns is not null;

    private bool HasChildRowContent => ChildRowContent is not null;

    private bool HasDefaultChildRowColumn => HasChildRowContent && GridContext.ChildRowColumn is null;

    private int ChildRowColSpan => GridContext.Entries.Count + (HasDefaultChildRowColumn ? 1 : 0);

    private bool AreAllBoundChildRowsExpanded
    {
        get
        {
            var items = BoundItems;
            return items.Count > 0 && items.All(IsChildRowExpanded);
        }
    }

    private object? GetItemKey(TItem item)
    {
        return ItemKey?.Invoke(item) ?? item;
    }

    private object GetPrimaryRowRenderKey(TItem item)
    {
        return ("row", GetItemKey(item));
    }

    private object GetChildRowRenderKey(TItem item)
    {
        return ("child", GetItemKey(item));
    }

    private object GetDescendantFilterProbeRenderKey(TItem item)
    {
        return ("probe", GetItemKey(item));
    }

    private string? InheritedFilterText => string.IsNullOrWhiteSpace(_currentFilterText)
        ? ParentFilterContext?.FilterText
        : _currentFilterText;

    private bool HasActiveFilter => !string.IsNullOrWhiteSpace(InheritedFilterText);

    private bool HasExactFilterMatch
    {
        get
        {
            EnsureFilterCache();
            return _hasExactFilterMatchCache!.Value;
        }
    }

    private string? EffectiveFilterText => ParentFilterOverride ? null : InheritedFilterText;

    private string EffectiveHighlightClass => !string.IsNullOrWhiteSpace(HighlightClass)
        ? HighlightClass
        : ParentFilterContext?.HighlightClass ?? DefaultHighlightCss;

    private IReadOnlyList<TItem> BoundItems => _boundItemsCache ??= ApplySorting((Items ?? []).Where(IsItemVisible)).ToArray();

    /// <summary>
    /// Drops the cached filter and sort results. Filtering is evaluated once per state change instead of
    /// per item and render, which keeps re-renders triggered by unrelated components (e.g. drawers) cheap.
    /// </summary>
    private void InvalidateFilterCache()
    {
        _lastFilterCacheKey = GetFilterCacheKey();
        _descendantFilterProbeVersion++;
        _hasExactFilterMatchCache = null;
        _filterMatchesCache = null;
        _boundItemsCache = null;
    }

    private string TableCssClass => $"{(string.IsNullOrWhiteSpace(TableClass) ? DefaultTableClass : TableClass)} {AdditionalTableClass}".Trim();

    private string HeaderCssClass => $"{(string.IsNullOrWhiteSpace(HeaderClass) ? DefaultHeaderClass : HeaderClass)} {AdditionalHeaderClass}".Trim();

    private string HeaderRowCssClass => $"{(string.IsNullOrWhiteSpace(HeaderRowClass) ? DefaultHeaderRowClass : HeaderRowClass)} {AdditionalHeaderRowClass}".Trim();

    private string GetHeaderCellCssClass(KalGridColumn<TItem> column)
    {
        return $"{AdditionalHeaderCellClass} {column.HeaderCssClass}".Trim();
    }

    private string? GetHeaderAriaSort(KalGridColumn<TItem> column)
    {
        return _sortDirections.TryGetValue(column, out var direction)
            ? direction == KalDataGridSortDirection.Descending ? "descending" : "ascending"
            : column.IsSortable ? "none" : null;
    }

    private string? GetSortIcon(KalGridColumn<TItem> column)
    {
        if (!_sortDirections.TryGetValue(column, out var direction))
        {
            return null;
        }

        return direction == KalDataGridSortDirection.Descending
            ? column.SortIconDescending ?? SortIconDescending
            : column.SortIconAscending ?? SortIconAscending;
    }

    private string? GetSortIconClass(KalGridColumn<TItem> column)
    {
        if (!_sortDirections.TryGetValue(column, out var direction))
        {
            return null;
        }

        return direction == KalDataGridSortDirection.Descending
            ? column.SortIconDescendingClass ?? SortIconDescendingClass
            : column.SortIconAscendingClass ?? SortIconAscendingClass;
    }

    private string GetSortButtonAriaLabel(KalGridColumn<TItem> column)
    {
        if (!_sortDirections.TryGetValue(column, out var direction))
        {
            return $"Nach {column.HeaderText} absteigend sortieren";
        }

        return direction == KalDataGridSortDirection.Descending
            ? $"Nach {column.HeaderText} aufsteigend sortieren"
            : $"Sortierung nach {column.HeaderText} entfernen";
    }

    private Task ToggleSortAsync(KalGridColumn<TItem> column)
    {
        if (!_sortDirections.TryGetValue(column, out var direction))
        {
            _sortDirections[column] = KalDataGridSortDirection.Descending;
            _sortColumns.Add(column);
        }
        else if (direction == KalDataGridSortDirection.Descending)
        {
            _sortDirections[column] = KalDataGridSortDirection.Ascending;
        }
        else
        {
            _sortDirections.Remove(column);
            _sortColumns.Remove(column);
        }

        _boundItemsCache = null;
        return InvokeAsync(StateHasChanged);
    }

    private IEnumerable<TItem> ApplySorting(IEnumerable<TItem> items)
    {
        IOrderedEnumerable<TItem>? orderedItems = null;

        foreach (var column in _sortColumns)
        {
            var direction = _sortDirections[column];
            if (orderedItems is null)
            {
                orderedItems = direction == KalDataGridSortDirection.Descending
                    ? items.OrderByDescending(column.GetSortValue, SortValueComparer.Instance)
                    : items.OrderBy(column.GetSortValue, SortValueComparer.Instance);
            }
            else
            {
                orderedItems = direction == KalDataGridSortDirection.Descending
                    ? orderedItems.ThenByDescending(column.GetSortValue, SortValueComparer.Instance)
                    : orderedItems.ThenBy(column.GetSortValue, SortValueComparer.Instance);
            }
        }

        return orderedItems ?? items;
    }

    private sealed class SortValueComparer : IComparer<object?>
    {
        public static SortValueComparer Instance { get; } = new();

        public int Compare(object? x, object? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x is null)
            {
                return -1;
            }

            if (y is null)
            {
                return 1;
            }

            if (x is IComparable comparable)
            {
                return comparable.CompareTo(y);
            }

            return StringComparer.CurrentCulture.Compare(x.ToString(), y.ToString());
        }
    }

    private string BodyCssClass => $"{(string.IsNullOrWhiteSpace(BodyClass) ? DefaultBodyClass : BodyClass)} {AdditionalBodyClass}".Trim();

    private string RowCssClass => $"{(string.IsNullOrWhiteSpace(RowClass) ? DefaultRowClass : RowClass)} {AdditionalRowClass}".Trim();

    private string ExpandHeaderCellCssClass => $"{(string.IsNullOrWhiteSpace(ExpandHeaderCellClass) ? DefaultExpandHeaderCellClass : ExpandHeaderCellClass)} {AdditionalExpandHeaderCellClass}".Trim();

    private string ExpandAllButtonCssClass => $"{(string.IsNullOrWhiteSpace(ExpandAllButtonClass) ? DefaultExpandAllButtonClass : ExpandAllButtonClass)} {AdditionalExpandAllButtonClass}".Trim();

    private string ExpandCellCssClass => $"{(string.IsNullOrWhiteSpace(ExpandCellClass) ? DefaultExpandCellClass : ExpandCellClass)} {AdditionalExpandCellClass}".Trim();

    private string ExpandButtonCssClass => $"{(string.IsNullOrWhiteSpace(ExpandButtonClass) ? DefaultExpandButtonClass : ExpandButtonClass)} {AdditionalExpandButtonClass}".Trim();

    private string ChildRowCssClass => $"{(string.IsNullOrWhiteSpace(ChildRowClass) ? DefaultChildRowClass : ChildRowClass)} {AdditionalChildRowClass}".Trim();

    private string ChildCellCssClass => $"{(string.IsNullOrWhiteSpace(ChildCellClass) ? DefaultChildCellClass : ChildCellClass)} {AdditionalChildCellClass}".Trim();

    private string ChildContentCssClass => $"{(string.IsNullOrWhiteSpace(ChildContentClass) ? DefaultChildContentClass : ChildContentClass)} {AdditionalChildContentClass}".Trim();

    private IEnumerable<TItem> DescendantFilterProbeItems => HasChildRowContent && !ParentFilterOverride && HasActiveFilter
        ? (Items ?? []).Where(item => !IsLocalFilterMatch(item))
        : [];

    /// <summary>
    /// Renders the hidden child row content of items that don't match the filter themselves, so nested grids can
    /// report matches in their own items.
    /// </summary>
    private RenderFragment RenderDescendantFilterProbes()
    {
        return builder =>
        {
            builder.OpenComponent<KalDataGridDescendantFilterProbeHost>(0);
            builder.AddComponentParameter(1, nameof(KalDataGridDescendantFilterProbeHost.Version), HasChildRowContent ? _descendantFilterProbeVersion : -1);
            builder.AddComponentParameter(2, nameof(KalDataGridDescendantFilterProbeHost.ChildContent), RenderDescendantFilterProbeItems());
            builder.CloseComponent();
        };
    }

    private RenderFragment RenderDescendantFilterProbeItems()
    {
        return builder =>
        {
            foreach (var item in DescendantFilterProbeItems)
            {
                builder.OpenComponent<KalDataGridDescendantFilterProbe>(0);
                builder.SetKey(GetDescendantFilterProbeRenderKey(item));
                builder.AddComponentParameter(1, nameof(KalDataGridDescendantFilterProbe.Item), item);
                builder.AddComponentParameter(2, nameof(KalDataGridDescendantFilterProbe.FilterText), InheritedFilterText);
                builder.AddComponentParameter(3, nameof(KalDataGridDescendantFilterProbe.HasExactFilterMatch), HasExactFilterMatch);
                builder.AddComponentParameter(4, nameof(KalDataGridDescendantFilterProbe.Version), RefreshVersion);
                builder.AddComponentParameter(5, nameof(KalDataGridDescendantFilterProbe.ChildContent), RenderDescendantFilterProbeContent(item));
                builder.CloseComponent();
            }
        };
    }

    private RenderFragment RenderDescendantFilterProbeContent(TItem item)
    {
        return builder =>
        {
            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "hidden");
            builder.AddAttribute(2, "aria-hidden", "true");
            builder.OpenComponent<CascadingValue<KalDataGridDescendantContext>>(3);
            builder.AddComponentParameter(4, "Name", KalDataGridDescendantContext.CascadeName);
            builder.AddComponentParameter(5, "Value", new KalDataGridDescendantContext(false, CreateDescendantFilterMatchContext(item), IsFilterProbe: true));
            builder.AddComponentParameter(6, "IsFixed", true);
            builder.AddComponentParameter(7, "ChildContent", ChildRowContent!(item));
            builder.CloseComponent();
            builder.CloseElement();
        };
    }

    private bool ItemMatchesFilter(TItem item)
    {
        EnsureFilterCache();
        return _filterMatchesCache!.Contains(item);
    }

    /// <summary>
    /// Evaluates the filter for all items in a single pass. Exact matches take precedence over partial matches,
    /// so both result sets are collected and the effective one is picked afterwards.
    /// </summary>
    private void EnsureFilterCache()
    {
        if (_filterMatchesCache is not null && _hasExactFilterMatchCache is not null)
        {
            return;
        }

        var exactMatches = new HashSet<TItem>();
        var containsMatches = new HashSet<TItem>();
        var filterTerms = string.IsNullOrWhiteSpace(InheritedFilterText)
            ? []
            : GetFilterTerms(InheritedFilterText)
                .Select(KalDataGridSearch.Normalize)
                .Where(term => term.Length > 0)
                .ToArray();

        if (filterTerms.Length > 0)
        {
            var columns = GridContext.Columns;

            foreach (var item in Items ?? [])
            {
                var (isExactMatch, isContainsMatch) = EvaluateFilterTerms(item, columns, filterTerms);

                if (isExactMatch)
                {
                    exactMatches.Add(item);
                }

                if (isContainsMatch)
                {
                    containsMatches.Add(item);
                }
            }
        }

        _hasExactFilterMatchCache = exactMatches.Count > 0;

        var useExactMatches = ParentFilterContext?.HasExactFilterMatch ?? _hasExactFilterMatchCache.Value;
        _filterMatchesCache = useExactMatches ? exactMatches : containsMatches;
    }

    private static (bool IsExactMatch, bool IsContainsMatch) EvaluateFilterTerms(
        TItem item,
        IReadOnlyList<KalGridColumn<TItem>> columns,
        IReadOnlyList<string> normalizedFilterTerms)
    {
        var isContainsMatch = false;

        foreach (var column in columns)
        {
            var searchText = KalDataGridSearch.Normalize(column.GetSearchText(item));

            if (searchText.Length == 0)
            {
                continue;
            }

            foreach (var filterTerm in normalizedFilterTerms)
            {
                if (KalDataGridSearch.EqualsNormalized(searchText, filterTerm))
                {
                    return (true, true);
                }

                isContainsMatch = isContainsMatch || KalDataGridSearch.ContainsNormalized(searchText, filterTerm);
            }
        }

        return (false, isContainsMatch);
    }

    private static string[] GetFilterTerms(string filterText)
    {
        return filterText.Split(
            (char[]?)null,
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private bool IsLocalFilterMatch(TItem item)
    {
        return !string.IsNullOrWhiteSpace(EffectiveFilterText)
            && ItemMatchesFilter(item);
    }

    private bool HasDescendantFilterMatch(TItem item)
    {
        return _descendantFilterMatches.Contains(item);
    }

    private bool IsItemVisible(TItem item)
    {
        if (string.IsNullOrWhiteSpace(EffectiveFilterText))
        {
            return true;
        }

        return IsLocalFilterMatch(item) || HasDescendantFilterMatch(item);
    }

    private bool ShouldOverrideDescendantFilter(TItem item)
    {
        if (ParentFilterOverride)
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(InheritedFilterText)
            && ItemMatchesFilter(item);
    }

    private bool ShouldShowChildRow(TItem item)
    {
        if (!HasChildRowContent)
        {
            return false;
        }

        return IsChildRowExpanded(item)
            || (HasActiveFilter && (ShouldOverrideDescendantFilter(item) || HasDescendantFilterMatch(item)));
    }

    /// <summary>
    /// The value is cascaded as fixed: child row content is re-rendered by the grid anyway and then reads the
    /// current value, while a non-fixed cascade would re-render all of its subscribers on every grid render.
    /// </summary>
    private KalDataGridDescendantContext CreateChildRowDescendantContext(TItem item)
    {
        return new KalDataGridDescendantContext(
            ShouldOverrideDescendantFilter(item),
            CreateDescendantFilterMatchContext(item),
            IsFilterProbe: false);
    }

    private KalDataGridDescendantFilterMatchContext CreateDescendantFilterMatchContext(TItem item)
    {
        // Reusing the context per item keeps track of which nested grids reported matches across renders.
        var itemKey = GetItemKey(item) ?? NullItemKey;

        if (!_descendantFilterMatchContexts.TryGetValue(itemKey, out var context))
        {
            context = new KalDataGridDescendantFilterMatchContext(hasMatch => ReportDescendantFilterMatch(item, hasMatch));
            _descendantFilterMatchContexts[itemKey] = context;
        }

        return context;
    }

    private void ReportDescendantFilterMatch(TItem item, bool hasMatch)
    {
        var current = HasDescendantFilterMatch(item);
        if (current == hasMatch)
        {
            return;
        }

        if (hasMatch)
        {
            _descendantFilterMatches.Add(item);
        }
        else
        {
            _descendantFilterMatches.Remove(item);
        }

        _boundItemsCache = null;
        _ = InvokeAsync(StateHasChanged);
    }

    private void ReportFilterMatchToParent()
    {
        if (ParentDescendantFilterMatchContext is null)
        {
            return;
        }

        var hasMatch = ParentFilterOverride
            ? (Items?.Any() ?? false)
            : BoundItems.Any();

        if (_lastReportedFilterMatch == hasMatch)
        {
            return;
        }

        _lastReportedFilterMatch = hasMatch;
        ParentDescendantFilterMatchContext.ReportMatch(this, hasMatch);
    }

    private bool IsChildRowExpanded(TItem item)
    {
        return _expandedItemKeys.Contains(GetItemKey(item));
    }

    private Task ToggleChildRowAsync(TItem item)
    {
        var itemKey = GetItemKey(item);

        if (!_expandedItemKeys.Add(itemKey))
        {
            _expandedItemKeys.Remove(itemKey);
        }

        return InvokeAsync(StateHasChanged);
    }

    private string GetExpandButtonAriaLabel(bool isChildRowExpanded)
    {
        return isChildRowExpanded
            ? ExpandButtonExpandedAriaLabel
            : ExpandButtonCollapsedAriaLabel;
    }

    private string GetExpandAllButtonAriaLabel()
    {
        return AreAllBoundChildRowsExpanded
            ? ExpandAllButtonExpandedAriaLabel
            : ExpandAllButtonCollapsedAriaLabel;
    }

    private Task ToggleAllChildRowsAsync()
    {
        SetAllChildRowsExpanded(!AreAllBoundChildRowsExpanded);
        return InvokeAsync(StateHasChanged);
    }

    private KalDataGridChildRowHeaderTemplateContext CreateChildRowHeaderTemplateContext()
    {
        return new KalDataGridChildRowHeaderTemplateContext(
            AreAllBoundChildRowsExpanded,
            GetExpandAllButtonAriaLabel(),
            ToggleAllChildRowsAsync);
    }

    private KalDataGridChildRowToggleTemplateContext<TItem> CreateChildRowToggleTemplateContext(TItem item, bool isExpanded)
    {
        return new KalDataGridChildRowToggleTemplateContext<TItem>(
            item,
            isExpanded,
            GetExpandButtonAriaLabel(isExpanded),
            () => ToggleChildRowAsync(item));
    }

    private RenderFragment RenderChildRowHeader(KalToggleGridColumn<TItem> column)
    {
        return column.CellHeaderTemplate is not null
            ? column.CellHeaderTemplate(CreateChildRowHeaderTemplateContext())
            : RenderDefaultChildRowHeader();
    }

    private RenderFragment RenderChildRowToggle(KalToggleGridColumn<TItem> column, TItem item, bool isExpanded)
    {
        return column.CellTemplate is not null
            ? column.CellTemplate(CreateChildRowToggleTemplateContext(item, isExpanded))
            : RenderDefaultChildRowToggle(item, isExpanded);
    }

    private RenderFragment RenderDefaultChildRowHeader()
    {
        return builder =>
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "type", "button");
            builder.AddAttribute(2, "class", ExpandAllButtonCssClass);
            builder.AddAttribute(3, "aria-expanded", AreAllBoundChildRowsExpanded);
            builder.AddAttribute(4, "aria-label", GetExpandAllButtonAriaLabel());
            builder.AddAttribute(5, "onclick", EventCallback.Factory.Create(this, ToggleAllChildRowsAsync));
            builder.OpenComponent<KalIcon>(6);
            builder.AddAttribute(7, "Icon", AreAllBoundChildRowsExpanded ? Icons.AngleUp : Icons.AngleDown);
            builder.CloseComponent();
            builder.CloseElement();
        };
    }

    private RenderFragment RenderDefaultChildRowToggle(TItem item, bool isExpanded)
    {
        return builder =>
        {
            builder.OpenElement(0, "button");
            builder.AddAttribute(1, "type", "button");
            builder.AddAttribute(2, "class", ExpandButtonCssClass);
            builder.AddAttribute(3, "aria-expanded", isExpanded);
            builder.AddAttribute(4, "aria-label", GetExpandButtonAriaLabel(isExpanded));
            builder.AddAttribute(5, "onclick", EventCallback.Factory.Create(this, () => ToggleChildRowAsync(item)));
            builder.OpenComponent<KalIcon>(6);
            builder.AddAttribute(7, "Icon", isExpanded ? Icons.AngleDown : Icons.AngleRight);
            builder.CloseComponent();
            builder.CloseElement();
        };
    }

    private void SetAllChildRowsExpanded(bool isExpanded, bool propagateToDescendants = true)
    {
        foreach (var item in BoundItems)
        {
            var itemKey = GetItemKey(item);

            if (isExpanded)
            {
                _expandedItemKeys.Add(itemKey);
            }
            else
            {
                _expandedItemKeys.Remove(itemKey);
            }
        }

        if (propagateToDescendants)
        {
            _pendingDescendantExpansion = isExpanded;
        }
    }

    private Task OnParentChildRowExpansionRequested(KalDataGridChildRowExpansionRequest request)
    {
        if (request.Version <= _lastHandledParentExpansionVersion)
        {
            return Task.CompletedTask;
        }

        if (Items is null)
        {
            return Task.CompletedTask;
        }

        _lastHandledParentExpansionVersion = request.Version;
        SetAllChildRowsExpanded(request.IsExpanded);

        return InvokeAsync(StateHasChanged);
    }

    async Task IKalDataGridFilterContext.SetFilterTextAsync(string? filterText)
    {
        _currentFilterText = filterText;
        InvalidateFilterCache();
        ResetDescendantFilterStateIfNeeded();
        NotifyFilterStateChangedIfNeeded();
        await InvokeAsync(StateHasChanged);

        if (FilterTextChanged.HasDelegate)
        {
            await FilterTextChanged.InvokeAsync(_currentFilterText);
        }
    }

    Task IKalDataGridFilterContext.ClearFilterTextAsync()
    {
        return ((IKalDataGridFilterContext)this).SetFilterTextAsync(null);
    }

    protected override void OnInitialized()
    {
        GridContext.StateChanged += OnGridContextStateChanged;
    }

    /// <summary>
    /// Re-evaluates filtering and sorting, e.g. after properties of existing items were changed in place.
    /// </summary>
    public Task RefreshAsync()
    {
        InvalidateFilterCache();
        _refreshVersion++;
        NotifyFilterStateChangedIfNeeded();
        return InvokeAsync(StateHasChanged);
    }

    protected override void OnParametersSet()
    {
        foreach (var column in _sortColumns.Where(column => !column.IsSortable).ToArray())
        {
            _sortColumns.Remove(column);
            _sortDirections.Remove(column);
            _boundItemsCache = null;
        }

        if (!string.Equals(FilterText, _lastFilterTextParameter, StringComparison.Ordinal))
        {
            _currentFilterText = FilterText;
            _lastFilterTextParameter = FilterText;
        }

        if (TryUpdateItemsSnapshot() || _lastFilterCacheKey != GetFilterCacheKey())
        {
            InvalidateFilterCache();
        }

        ResetDescendantFilterStateIfNeeded();

        if (!ReferenceEquals(_subscribedParentFilterContext, ParentFilterContext))
        {
            if (_subscribedParentFilterContext is not null)
            {
                _subscribedParentFilterContext.FilterStateChanged -= OnParentFilterStateChanged;
            }

            _subscribedParentFilterContext = ParentFilterContext;

            if (_subscribedParentFilterContext is not null)
            {
                _subscribedParentFilterContext.FilterStateChanged += OnParentFilterStateChanged;
            }
        }

        NotifyFilterStateChangedIfNeeded();

        if (!ReferenceEquals(_subscribedParentChildRowExpansionContext, ParentChildRowExpansionContext))
        {
            if (_subscribedParentChildRowExpansionContext is not null)
            {
                _subscribedParentChildRowExpansionContext.ExpansionRequested -= OnParentChildRowExpansionRequested;
            }

            _subscribedParentChildRowExpansionContext = ParentChildRowExpansionContext;

            if (_subscribedParentChildRowExpansionContext is not null)
            {
                _subscribedParentChildRowExpansionContext.ExpansionRequested += OnParentChildRowExpansionRequested;
            }
        }

        if (ParentChildRowExpansionContext?.CurrentRequest is { } request
            && request.Version > _lastHandledParentExpansionVersion)
        {
            if (Items is not null)
            {
                _lastHandledParentExpansionVersion = request.Version;
                SetAllChildRowsExpanded(request.IsExpanded);
            }
        }

        if (!ExpandChildRowsByDefault || _defaultExpansionApplied || Items is null)
        {
            return;
        }

        foreach (var item in Items)
        {
            _expandedItemKeys.Add(GetItemKey(item));
        }

        _defaultExpansionApplied = true;

        ReportFilterMatchToParent();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        _hasRendered = true;
        ReportFilterMatchToParent();

        if (_pendingDescendantExpansion is not { } isExpanded)
        {
            return;
        }

        _pendingDescendantExpansion = null;
        await ChildRowExpansionContext.RequestExpansionAsync(isExpanded);
    }

    private (string? FilterText, bool? ParentHasExactFilterMatch, int ParentRefreshVersion) GetFilterCacheKey()
    {
        return (InheritedFilterText, ParentFilterContext?.HasExactFilterMatch, ParentFilterContext?.RefreshVersion ?? 0);
    }

    /// <summary>
    /// Parents usually pass the same items again when they re-render for unrelated reasons, e.g. a drawer toggle.
    /// Comparing the item instances is cheap compared to filtering, so cached results are kept in that case.
    /// </summary>
    private bool TryUpdateItemsSnapshot()
    {
        var items = Items as IReadOnlyCollection<TItem> ?? Items?.ToArray() ?? [];
        var hasChanged = items.Count != _itemsSnapshot.Length;

        if (!hasChanged)
        {
            var index = 0;

            foreach (var item in items)
            {
                if (!IsSameItemInstance(_itemsSnapshot[index++], item))
                {
                    hasChanged = true;
                    break;
                }
            }
        }

        if (hasChanged)
        {
            _itemsSnapshot = items.ToArray();
        }

        return hasChanged;
    }

    private static bool IsSameItemInstance(TItem previous, TItem current)
    {
        return typeof(TItem).IsValueType
            ? EqualityComparer<TItem>.Default.Equals(previous, current)
            : ReferenceEquals(previous, current);
    }

    private void ResetDescendantFilterStateIfNeeded()
    {
        if (_lastParentFilterOverride == ParentFilterOverride
            && string.Equals(_lastFilterStateText, InheritedFilterText, StringComparison.Ordinal))
        {
            return;
        }

        _descendantFilterProbeVersion++;
        _descendantFilterMatches.Clear();
        _descendantFilterMatchContexts.Clear();
        _boundItemsCache = null;
        _lastReportedFilterMatch = null;
        _lastFilterStateText = InheritedFilterText;
        _lastParentFilterOverride = ParentFilterOverride;
    }

    /// <summary>
    /// The grid cascades itself as a fixed value, because a non-fixed cascade of a reference type re-renders every
    /// subscriber on every grid render. Descendants are instead notified only when the filter state they read changes.
    /// </summary>
    private void NotifyFilterStateChangedIfNeeded()
    {
        var filterState = (InheritedFilterText, EffectiveHighlightClass, HasExactFilterMatch);

        if (_lastNotifiedFilterState == filterState)
        {
            return;
        }

        var hasNotifiedBefore = _lastNotifiedFilterState is not null;
        _lastNotifiedFilterState = filterState;

        if (!hasNotifiedBefore || _filterStateChangedHandlers.Count == 0)
        {
            return;
        }

        // Handlers may re-render and thereby dispose subscribers, so they are invoked on a copy.
        foreach (var handler in _filterStateChangedHandlers.ToArray())
        {
            handler();
        }
    }

    private void OnParentFilterStateChanged()
    {
        InvalidateFilterCache();
        ResetDescendantFilterStateIfNeeded();
        NotifyFilterStateChangedIfNeeded();
        _ = InvokeAsync(StateHasChanged);
    }

    private void OnGridContextStateChanged()
    {
        InvalidateFilterCache();
        NotifyFilterStateChangedIfNeeded();

        // Columns register while a grid renders for the first time. A probe without own child rows renders no rows
        // and reports its matches after that first render anyway, so re-rendering it would be wasted work.
        if (IsDescendantFilterProbe && !HasChildRowContent && !_hasRendered)
        {
            return;
        }

        _ = InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        GridContext.StateChanged -= OnGridContextStateChanged;

        if (_subscribedParentFilterContext is not null)
        {
            _subscribedParentFilterContext.FilterStateChanged -= OnParentFilterStateChanged;
        }

        if (_subscribedParentChildRowExpansionContext is not null)
        {
            _subscribedParentChildRowExpansionContext.ExpansionRequested -= OnParentChildRowExpansionRequested;
        }
    }
}
