using Microsoft.AspNetCore.Components;

namespace SoftwareThingies.KalBlazor.Core.DataGrid;

public partial class KalDataGrid<TItem> : IDisposable, IKalDataGridFilterContext
{
    private const string DescendantFilterOverrideCascadeName = "KalDataGridDescendantFilterOverride";
    private const string DescendantFilterMatchCascadeName = "KalDataGridDescendantFilterMatch";
    private const string DefaultTableClass = "w-full min-w-full border-collapse text-sm";
    private const string DefaultHeaderClass = "";
    private const string DefaultHeaderRowClass = "";
    private const string DefaultBodyClass = "divide-y";
    private const string DefaultRowClass = "transition-colors";
    private const string DefaultExpandHeaderCellClass = "w-0 px-3 py-3";
    private const string DefaultExpandAllButtonClass = "inline-flex h-7 w-7 items-center justify-center rounded-md text-slate-500 transition-colors hover:bg-slate-100 hover:text-slate-900 focus:outline-none focus:ring-2 focus:ring-amber-200";
    private const string DefaultExpandCellClass = "w-0 px-3 py-3 align-top";
    private const string DefaultExpandButtonClass = "inline-flex h-7 w-7 items-center justify-center rounded-md text-slate-500 transition-colors hover:bg-slate-100 hover:text-slate-900 focus:outline-none focus:ring-2 focus:ring-amber-200";
    private const string DefaultChildRowClass = "kal-data-grid-child-row";
    private const string DefaultChildCellClass = "bg-slate-50 px-4 py-4";
    private const string DefaultChildContentClass = "w-full";
    protected override string ComponentClass => "kal-data-grid";

    protected override string DefaultClass => "w-full";

    private readonly HashSet<object?> _expandedItemKeys = [];
    private readonly HashSet<TItem> _descendantFilterMatches = [];
    private bool _defaultExpansionApplied;
    private bool? _pendingDescendantExpansion;
    private KalDataGridChildRowExpansionContext? _subscribedParentChildRowExpansionContext;
    private int _lastHandledParentExpansionVersion;
    private string? _currentFilterText;
    private string? _lastFilterStateText;
    private string? _lastFilterTextParameter;
    private bool _lastParentFilterOverride;
    private bool? _lastReportedFilterMatch;

    private KalDataGridContext<TItem> GridContext { get; } = new();

    private KalDataGridChildRowExpansionContext ChildRowExpansionContext { get; } = new();

    string? IKalDataGridFilterContext.FilterText => InheritedFilterText;

    [CascadingParameter]
    private KalDataGridChildRowExpansionContext? ParentChildRowExpansionContext { get; set; }

    [CascadingParameter]
    private IKalDataGridFilterContext? ParentFilterContext { get; set; }

    [CascadingParameter(Name = DescendantFilterOverrideCascadeName)]
    private bool ParentFilterOverride { get; set; }

    [CascadingParameter(Name = DescendantFilterMatchCascadeName)]
    private KalDataGridDescendantFilterMatchContext? ParentDescendantFilterMatchContext { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

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
            var items = BoundItems.ToArray();
            return items.Length > 0 && items.All(IsChildRowExpanded);
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

    private string? EffectiveFilterText => ParentFilterOverride ? null : InheritedFilterText;

    private IEnumerable<TItem> BoundItems => (Items ?? []).Where(IsItemVisible);

    private string TableCssClass => $"{(string.IsNullOrWhiteSpace(TableClass) ? DefaultTableClass : TableClass)} {AdditionalTableClass}".Trim();

    private string HeaderCssClass => $"{(string.IsNullOrWhiteSpace(HeaderClass) ? DefaultHeaderClass : HeaderClass)} {AdditionalHeaderClass}".Trim();

    private string HeaderRowCssClass => $"{(string.IsNullOrWhiteSpace(HeaderRowClass) ? DefaultHeaderRowClass : HeaderRowClass)} {AdditionalHeaderRowClass}".Trim();

    private string GetHeaderCellCssClass(KalGridColumn<TItem> column)
    {
        return $"{AdditionalHeaderCellClass} {column.HeaderCssClass}".Trim();
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

    private bool ItemMatchesFilter(TItem item, string filterText)
    {
        return GridContext.Columns.Any(column =>
            column.GetSearchText(item).Contains(filterText, StringComparison.CurrentCultureIgnoreCase));
    }

    private bool IsLocalFilterMatch(TItem item)
    {
        return !string.IsNullOrWhiteSpace(EffectiveFilterText)
            && ItemMatchesFilter(item, EffectiveFilterText!);
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
            && ItemMatchesFilter(item, InheritedFilterText!);
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

    private KalDataGridDescendantFilterMatchContext CreateDescendantFilterMatchContext(TItem item)
    {
        return new KalDataGridDescendantFilterMatchContext(hasMatch => ReportDescendantFilterMatch(item, hasMatch));
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
        ParentDescendantFilterMatchContext.ReportMatch(hasMatch);
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
        foreach (var item in BoundItems.ToArray())
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

    protected override void OnParametersSet()
    {
        if (!string.Equals(FilterText, _lastFilterTextParameter, StringComparison.Ordinal))
        {
            _currentFilterText = FilterText;
            _lastFilterTextParameter = FilterText;
        }

        if (_lastParentFilterOverride != ParentFilterOverride
            || !string.Equals(_lastFilterStateText, InheritedFilterText, StringComparison.Ordinal))
        {
            _descendantFilterMatches.Clear();
            _lastReportedFilterMatch = null;
            _lastFilterStateText = InheritedFilterText;
            _lastParentFilterOverride = ParentFilterOverride;
        }

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
        ReportFilterMatchToParent();

        if (_pendingDescendantExpansion is not { } isExpanded)
        {
            return;
        }

        _pendingDescendantExpansion = null;
        await ChildRowExpansionContext.RequestExpansionAsync(isExpanded);
    }

    private void OnGridContextStateChanged()
    {
        _ = InvokeAsync(StateHasChanged);
    }

    public void Dispose()
    {
        GridContext.StateChanged -= OnGridContextStateChanged;

        if (_subscribedParentChildRowExpansionContext is not null)
        {
            _subscribedParentChildRowExpansionContext.ExpansionRequested -= OnParentChildRowExpansionRequested;
        }
    }
}
