using Bunit;
using Microsoft.Extensions.DependencyInjection;
using SoftwareThingies.KalBlazor.Core.Tests.TestComponents;
using Xunit.Abstractions;

namespace SoftwareThingies.KalBlazor.Core.Tests.Performance;

/// <summary>
/// Measures the grid with a large data set that an active filter narrows down to a few rows.
/// </summary>
public sealed class DataGridFilterPerformanceTests(ITestOutputHelper output)
{
    private const int ItemCount = 5_000;
    private const string NarrowFilter = "Zielgruppe";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ActiveFilter_NarrowsLargeDataSet_ToMatchingRows(bool withChildRows)
    {
        using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddSingleton(new RenderCounter());

        var cut = context.Render<AccountGrid>(parameters => parameters
            .Add(grid => grid.Items, TestData.CreateAccounts(ItemCount))
            .Add(grid => grid.FilterText, NarrowFilter)
            .Add(grid => grid.WithChildRows, withChildRows));

        var visibleRows = cut.Find("div.kal-data-grid > table > tbody").Children
            .Where(row => !row.ClassList.Contains("kal-data-grid-child-row"))
            .ToArray();
        Assert.Equal(ItemCount / 500, visibleRows.Length);
    }

    [Theory]
    [InlineData(false, 500)]
    [InlineData(true, 2_000)]
    public async Task ApplyingFilter_OnLargeDataSet(bool withChildRows, double maxMedianMs)
    {
        await using var scope = await RenderHostAsync(withChildRows, NarrowFilter);
        var renderer = scope.Renderer;

        var result = await PerformanceMeasurement.MeasureAsync(
            output,
            $"Filter anwenden ({ItemCount} Einträge, Kindzeilen: {withChildRows})",
            iterations: 5,
            async () =>
            {
                renderer.ResetStatistics();
                await renderer.ClickAsync("perf-apply-filter");
            },
            beforeEach: () => renderer.ClickAsync("perf-clear-filter"));

        output.WriteLine($"Pro Filtervorgang: {renderer.RenderedComponents} gerenderte Komponenten, {renderer.DomEdits} DOM-Änderungen");
        Assert.True(result.MedianMs < maxMedianMs, $"Filtern dauerte {result.MedianMs:F2} ms");
    }

    [Theory]
    [InlineData(false, 1_500)]
    [InlineData(true, 1_500)]
    public async Task ClearingFilter_OnLargeDataSet(bool withChildRows, double maxMedianMs)
    {
        await using var scope = await RenderHostAsync(withChildRows, NarrowFilter);
        var renderer = scope.Renderer;

        // Clearing renders all rows again, so this mostly measures rendering 5,000 rows.
        var result = await PerformanceMeasurement.MeasureAsync(
            output,
            $"Filter entfernen ({ItemCount} Einträge, Kindzeilen: {withChildRows})",
            iterations: 5,
            () => renderer.ClickAsync("perf-clear-filter"),
            beforeEach: () => renderer.ClickAsync("perf-apply-filter"));

        Assert.True(result.MedianMs < maxMedianMs, $"Filter entfernen dauerte {result.MedianMs:F2} ms");
    }

    [Theory]
    [InlineData(false, 150)]
    [InlineData(true, 200)]
    public async Task ParentReRender_WithActiveFilter_OnLargeDataSet(bool withChildRows, double maxMedianMs)
    {
        await using var scope = await RenderHostAsync(withChildRows, NarrowFilter);
        var renderer = scope.Renderer;

        // A parent re-render without grid-relevant changes, e.g. caused by a drawer bound via @bind-InitialState.
        var result = await PerformanceMeasurement.MeasureAsync(
            output,
            $"Eltern-Re-Render mit aktivem Filter ({ItemCount} Einträge, Kindzeilen: {withChildRows})",
            iterations: 20,
            () => renderer.ClickAsync("perf-rerender"));

        var renders = result.Iterations + 2;
        var componentsPerRender = renderer.RenderedComponents / renders;
        output.WriteLine($"Pro Re-Render: {componentsPerRender} gerenderte Komponenten, {renderer.DomEdits / renders} DOM-Änderungen");

        Assert.True(result.MedianMs < maxMedianMs, $"Re-Render dauerte {result.MedianMs:F2} ms");
        Assert.True(componentsPerRender < 500, $"{componentsPerRender} Komponenten pro Re-Render");
    }

    private static Task<RendererScope> RenderHostAsync(bool withChildRows, string? filterText)
    {
        return RendererScope.RenderAsync<FilterGridHost>(
            new RenderCounter(),
            new GridPageState
            {
                Items = TestData.CreateAccounts(ItemCount),
                FilterText = filterText,
                WithChildRows = withChildRows
            });
    }
}
