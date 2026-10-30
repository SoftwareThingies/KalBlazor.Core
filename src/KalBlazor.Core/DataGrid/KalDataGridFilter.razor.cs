using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace SoftwareThingies.KalBlazor.Core.DataGrid;

public partial class KalDataGridFilter : BaseComponent, IDisposable
{
    private const string DefaultInputClass = "w-full rounded-md border border-slate-300 bg-white px-3 py-2 text-sm text-slate-950 outline-none transition-colors placeholder:text-slate-400 focus:border-amber-500 focus:ring-2 focus:ring-amber-200";
    private const string DefaultResetButtonClass = "rounded-md border border-slate-300 bg-white px-3 py-2 text-sm font-medium text-slate-700 transition-colors hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-50";
    private string? _pendingFilterText;
    private string? _lastAppliedFilterText;
    private bool _hasInitializedFilterText;
    private CancellationTokenSource? _filterDelayCancellation;

    [Parameter]
    public int OnChangeDebounceMilliseconds { get; set; } = 300;

    protected override string ComponentClass => "kal-data-grid-filter";

    protected override string DefaultClass => "flex items-center gap-2 border-b border-slate-200 bg-white p-3";

    [Parameter]
    public string Placeholder { get; set; } = "Filter...";

    [Parameter]
    public KalDataGridFilterMode FilterMode { get; set; } = KalDataGridFilterMode.OnChange;

    [Parameter]
    public string? InputClass { get; set; }

    [Parameter]
    public string? AdditionalInputClass { get; set; }

    [Parameter]
    public bool ShowResetButton { get; set; } = true;

    [Parameter]
    public string ResetButtonText { get; set; } = "Reset";

    [Parameter]
    public string? ResetButtonClass { get; set; }

    [Parameter]
    public string? AdditionalResetButtonClass { get; set; }

    [CascadingParameter]
    internal IKalDataGridFilterContext? FilterContext { get; set; }

    private string InputCssClass => $"{(string.IsNullOrWhiteSpace(InputClass) ? DefaultInputClass : InputClass)} {AdditionalInputClass}".Trim();

    private string ResetButtonCssClass => $"{(string.IsNullOrWhiteSpace(ResetButtonClass) ? DefaultResetButtonClass : ResetButtonClass)} {AdditionalResetButtonClass}".Trim();

    private bool HasFilterText => !string.IsNullOrWhiteSpace(FilterContext?.FilterText);

    private string? PendingFilterText => _pendingFilterText;

    protected override void OnParametersSet()
    {
        if (FilterContext is null)
        {
            throw new InvalidOperationException($"{nameof(KalDataGridFilter)} must be placed inside the {nameof(KalDataGrid<object>)} Filter template.");
        }

        if (!_hasInitializedFilterText
            || !string.Equals(FilterContext.FilterText, _lastAppliedFilterText, StringComparison.Ordinal))
        {
            _pendingFilterText = FilterContext.FilterText;
            _lastAppliedFilterText = FilterContext.FilterText;
            _hasInitializedFilterText = true;
        }
    }

    private Task OnInput(ChangeEventArgs args)
    {
        _pendingFilterText = args.Value?.ToString();
        _filterDelayCancellation?.Cancel();

        if (string.IsNullOrWhiteSpace(_pendingFilterText))
        {
            return FilterContext!.ClearFilterTextAsync();
        }

        return FilterMode == KalDataGridFilterMode.OnChange
            ? ApplyFilterAfterDelayAsync(_pendingFilterText)
            : Task.CompletedTask;
    }

    private async Task ApplyFilterAfterDelayAsync(string? filterText)
    {
        var cancellation = new CancellationTokenSource();
        _filterDelayCancellation = cancellation;

        try
        {
            await Task.Delay(Math.Max(0, OnChangeDebounceMilliseconds), cancellation.Token);
            if (!cancellation.IsCancellationRequested)
            {
                await FilterContext!.SetFilterTextAsync(filterText);
            }
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        finally
        {
            if (ReferenceEquals(_filterDelayCancellation, cancellation))
            {
                _filterDelayCancellation = null;
            }

            cancellation.Dispose();
        }
    }

    private Task OnKeyDown(KeyboardEventArgs args)
    {
        return args.Key == "Enter" && FilterMode == KalDataGridFilterMode.OnEnterKey
            ? ApplyFilterAsync()
            : Task.CompletedTask;
    }

    private Task ResetAsync()
    {
        _filterDelayCancellation?.Cancel();
        _pendingFilterText = null;
        return FilterContext!.ClearFilterTextAsync();
    }

    private Task ApplyFilterAsync()
    {
        _filterDelayCancellation?.Cancel();
        return FilterContext!.SetFilterTextAsync(_pendingFilterText);
    }

    public void Dispose()
    {
        _filterDelayCancellation?.Cancel();
        _filterDelayCancellation?.Dispose();
    }
}
