namespace SoftwareThingies.KalBlazor.Core.DataGrid;

internal interface IKalDataGridFilterContext
{
    string? FilterText { get; }

    bool HasExactFilterMatch { get; }

    Task SetFilterTextAsync(string? filterText);

    Task ClearFilterTextAsync();
}
