using Microsoft.AspNetCore.Components;

namespace SoftwareThingies.KalBlazor.Core.DataGrid;

public partial class KalToggleGridColumn<TItem> : IDisposable
{
    private const string DefaultCellClass = "w-0 px-3 py-3 align-top";

    protected override string ComponentClass => "kal-toggle-grid-column";

    protected override string DefaultClass => "w-0 px-3 py-3";

    protected override string DynamicClass => WidthClass ?? string.Empty;

    [CascadingParameter]
    internal KalDataGridContext<TItem>? GridContext { get; set; }

    [Parameter]
    public RenderFragment<KalDataGridChildRowHeaderTemplateContext>? CellHeaderTemplate { get; set; }

    [Parameter]
    public RenderFragment<KalDataGridChildRowToggleTemplateContext<TItem>>? CellTemplate { get; set; }

    [Parameter]
    public string Scope { get; set; } = "col";

    [Parameter]
    public string? WidthClass { get; set; }

    [Parameter]
    public string? HeaderClass { get; set; }

    [Parameter]
    public string? AdditionalHeaderClass { get; set; }

    [Parameter]
    public string? CellClass { get; set; }

    [Parameter]
    public string? AdditionalCellClass { get; set; }

    internal string HeaderCssClass
    {
        get
        {
            var effectiveClass = !string.IsNullOrWhiteSpace(HeaderClass)
                ? HeaderClass
                : CssClass;

            return $"{effectiveClass} {AdditionalHeaderClass}".Trim();
        }
    }

    internal IReadOnlyDictionary<string, object>? HeaderAttributes => FilteredAdditionalAttributes;

    internal string CellCssClass
    {
        get
        {
            var effectiveClass = !string.IsNullOrWhiteSpace(CellClass)
                ? CellClass
                : DefaultCellClass;

            return $"{effectiveClass} {AdditionalCellClass}".Trim();
        }
    }

    internal IReadOnlyDictionary<string, object>? CellAttributes => FilteredAdditionalAttributes;

    protected override void OnInitialized()
    {
        GridContext?.Register(this);
    }

    public void Dispose()
    {
        GridContext?.Unregister(this);
    }
}
