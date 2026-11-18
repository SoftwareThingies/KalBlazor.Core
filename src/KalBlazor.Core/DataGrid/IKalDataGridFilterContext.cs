namespace SoftwareThingies.KalBlazor.Core.DataGrid;

internal interface IKalDataGridFilterContext
{
    string? FilterText { get; }

    string HighlightClass { get; }

    bool HasExactFilterMatch { get; }

    /// <summary>
    /// Changes when the grid or one of its ancestors was refreshed explicitly.
    /// </summary>
    int RefreshVersion { get; }

    /// <summary>
    /// Raised when <see cref="FilterText" />, <see cref="HighlightClass" /> or <see cref="HasExactFilterMatch" /> changed.
    /// </summary>
    event Action? FilterStateChanged;

    Task SetFilterTextAsync(string? filterText);

    Task ClearFilterTextAsync();
}
