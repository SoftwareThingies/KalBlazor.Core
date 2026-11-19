using Bunit;
using Microsoft.Extensions.DependencyInjection;
using SoftwareThingies.KalBlazor.Core.DataGrid;
using SoftwareThingies.KalBlazor.Core.Tests.TestComponents;
using Xunit.Abstractions;

namespace SoftwareThingies.KalBlazor.Core.Tests.Performance;

/// <summary>
/// Opens and closes a drawer next to a large grid that an active filter narrows down to a few rows.
/// </summary>
public sealed class DrawerWithFilteredGridPerformanceTests(ITestOutputHelper output)
{
    private const int ItemCount = 5_000;
    private const string NarrowFilter = "Zielgruppe";
    private const int VisibleRowCount = ItemCount / 500;

    public static TheoryData<bool, bool, string?> Scenarios => new()
    {
        { false, false, null },
        { false, false, NarrowFilter },
        { false, true, NarrowFilter },
        { true, false, NarrowFilter },
        { true, true, NarrowFilter },
    };

    [Theory]
    [MemberData(nameof(Scenarios))]
    public async Task TogglingDrawer_WithLargeGrid(bool withChildRows, bool bindDrawerState, string? filterText)
    {
        await using var scope = await RendererScope.RenderAsync<LayoutHost>(
            new RenderCounter(),
            CreateState(withChildRows, bindDrawerState, filterText));
        var renderer = scope.Renderer;

        var result = await PerformanceMeasurement.MeasureAsync(
            output,
            $"Drawer auf/zu (Filter: {filterText ?? "-"}, Kindzeilen: {withChildRows}, @bind-InitialState: {bindDrawerState})",
            iterations: 20,
            async () =>
            {
                await renderer.ClickAsync("perf-drawer-toggle");
                await renderer.ClickAsync("perf-drawer-toggle");
            });

        var toggles = (result.Iterations + 2) * 2;
        var componentsPerToggle = renderer.RenderedComponents / toggles;
        output.WriteLine($"Pro Drawer-Wechsel: {componentsPerToggle} gerenderte Komponenten, {renderer.DomEdits / toggles} DOM-Änderungen");

        Assert.True(result.MedianMs < 100, $"Drawer-Wechsel dauerte {result.MedianMs:F2} ms");
        Assert.True(componentsPerToggle < 300, $"{componentsPerToggle} Komponenten pro Drawer-Wechsel");
    }

    [Theory]
    [MemberData(nameof(Scenarios))]
    public void TogglingDrawer_DoesNotReRenderHiddenProbes(bool withChildRows, bool bindDrawerState, string? filterText)
    {
        using var context = new BunitContext();
        var counter = new RenderCounter();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        context.Services.AddSingleton(counter);
        context.Services.AddSingleton(CreateState(withChildRows, bindDrawerState, filterText));

        var cut = context.Render<LayoutHost>();
        var trigger = cut.Find("button.perf-drawer-toggle");
        var nestedGrids = cut.FindComponents<KalDataGrid<Invoice>>();
        var nestedRendersBefore = nestedGrids.Select(grid => grid.RenderCount).ToArray();
        output.WriteLine($"DOM-Elemente: {cut.FindAll("*").Count}, verschachtelte Grids: {nestedGrids.Count}");
        counter.Reset();

        trigger.Click();
        Assert.Contains("kal-drawer-open", cut.Find("aside.kal-drawer").ClassName);
        trigger.Click();
        Assert.Contains("kal-drawer-closed", cut.Find("aside.kal-drawer").ClassName);

        var reRenderedNestedGrids = nestedGrids.Where((grid, index) => grid.RenderCount != nestedRendersBefore[index]).Count();
        var visibleRowCount = filterText is null ? ItemCount : VisibleRowCount;
        output.WriteLine($"Gerenderte Grid-Zellen: {counter.CellRenders}, neu gerenderte verschachtelte Grids: {reRenderedNestedGrids} von {nestedGrids.Count}");

        // Only visible rows may re-render (once per toggle when the page binds the drawer state), never the hidden probes.
        Assert.True(reRenderedNestedGrids <= visibleRowCount, $"{reRenderedNestedGrids} verschachtelte Grids wurden neu gerendert");
        Assert.True(counter.CellRenders <= 2 * visibleRowCount, $"{counter.CellRenders} Zellen wurden neu gerendert");
    }

    private static GridPageState CreateState(bool withChildRows, bool bindDrawerState, string? filterText)
    {
        return new GridPageState
        {
            Items = TestData.CreateAccounts(ItemCount),
            FilterText = filterText,
            WithChildRows = withChildRows,
            BindDrawerState = bindDrawerState
        };
    }
}
