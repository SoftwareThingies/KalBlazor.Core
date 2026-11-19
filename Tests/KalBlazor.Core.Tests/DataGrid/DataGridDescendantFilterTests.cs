using AngleSharp.Dom;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using SoftwareThingies.KalBlazor.Core.DataGrid;
using SoftwareThingies.KalBlazor.Core.Tests.TestComponents;

namespace SoftwareThingies.KalBlazor.Core.Tests.DataGrid;

public sealed class DataGridDescendantFilterTests : BunitContext
{
    public DataGridDescendantFilterTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton(new RenderCounter());
    }

    [Fact]
    public void Filter_MatchingOnlyAChildItem_ShowsParentRowWithChildRow()
    {
        var items = TestData.CreateAccounts(200);

        var cut = Render<AccountGrid>(parameters => parameters
            .Add(grid => grid.Items, items)
            .Add(grid => grid.FilterText, "RE-00042-2")
            .Add(grid => grid.WithChildRows, true));

        var rows = GetTopLevelRows(cut, includeChildRows: true);
        var row = Assert.Single(GetTopLevelRows(cut));
        Assert.Contains("Kunde 00042", row.TextContent);
        Assert.Contains(rows, row => row.ClassList.Contains("kal-data-grid-child-row") && row.TextContent.Contains("RE-00042-2"));
    }

    [Fact]
    public void Filter_WithExactMatch_PrefersExactMatches()
    {
        var items = TestData.CreateAccounts(200);

        var cut = Render<AccountGrid>(parameters => parameters
            .Add(grid => grid.Items, items)
            .Add(grid => grid.FilterText, "Kunde00010"));

        var row = Assert.Single(GetTopLevelRows(cut));
        Assert.Contains("Kunde 00010", row.TextContent);
    }

    [Fact]
    public void Filter_WithoutExactMatch_ShowsPartialMatches()
    {
        var items = TestData.CreateAccounts(200);

        var cut = Render<AccountGrid>(parameters => parameters
            .Add(grid => grid.Items, items)
            .Add(grid => grid.FilterText, "Kunde0001"));

        // Normalized "Kunde00010" ... "Kunde00019" contain the term, "Kunde00001" does not.
        Assert.Equal(10, GetTopLevelRows(cut).Length);
    }

    [Fact]
    public void Probes_DoNotRenderNestedTables()
    {
        var items = TestData.CreateAccounts(200);

        var cut = Render<AccountGrid>(parameters => parameters
            .Add(grid => grid.Items, items)
            .Add(grid => grid.FilterText, "Zielgruppe")
            .Add(grid => grid.WithChildRows, true));

        Assert.Empty(GetTopLevelRows(cut));
        Assert.Single(cut.FindAll("table"));
    }

    [Fact]
    public void ParentReRender_WithUnchangedFilter_DoesNotReRenderProbes()
    {
        var items = TestData.CreateAccounts(200);
        var cut = Render<AccountGrid>(parameters => parameters
            .Add(grid => grid.Items, items)
            .Add(grid => grid.FilterText, "RE-00042-2")
            .Add(grid => grid.WithChildRows, true));
        var nestedGrids = cut.FindComponents<KalDataGrid<Invoice>>();
        var rendersBefore = nestedGrids.Select(grid => grid.RenderCount).ToArray();

        cut.Render();

        // Only the nested grid of the single visible child row re-renders, the hidden probes stay untouched.
        var reRenderedGrids = nestedGrids.Where((grid, index) => grid.RenderCount != rendersBefore[index]).ToArray();
        Assert.True(nestedGrids.Count > 100);
        Assert.Single(reRenderedGrids);
    }

    [Fact]
    public void ChangedItems_ReEvaluateProbes()
    {
        var cut = Render<AccountGrid>(parameters => parameters
            .Add(grid => grid.Items, TestData.CreateAccounts(200))
            .Add(grid => grid.FilterText, "RE-00042-2")
            .Add(grid => grid.WithChildRows, true));

        var changedItems = TestData.CreateAccounts(200)
            .Select(account => account.Id == 42
                ? account with { Invoices = [new Invoice("RE-99999-1", "2026-01", 1m, "Offen")] }
                : account)
            .ToList();
        cut.Render(parameters => parameters.Add(grid => grid.Items, changedItems));

        Assert.Empty(GetTopLevelRows(cut));
    }

    [Fact]
    public void FilterInput_UpdatesVisibleRowsNestedGridsAndHighlights()
    {
        var cut = Render<AccountGrid>(parameters => parameters
            .Add(grid => grid.Items, TestData.CreateAccounts(200))
            .Add(grid => grid.FilterText, "RE-00042-2")
            .Add(grid => grid.WithChildRows, true)
            .Add(grid => grid.ShowFilter, true));

        cut.Find("div.kal-data-grid-filter input").Input("RE-00043-1");

        cut.WaitForAssertion(() =>
        {
            var row = Assert.Single(GetTopLevelRows(cut));
            Assert.Contains("Kunde 00043", row.TextContent);
            Assert.Contains(
                GetTopLevelRows(cut, includeChildRows: true),
                childRow => childRow.ClassList.Contains("kal-data-grid-child-row") && childRow.TextContent.Contains("RE-00043-1"));
        }, TimeSpan.FromSeconds(5));

        cut.Find("div.kal-data-grid-filter input").Input("Kunde00044");

        cut.WaitForAssertion(() =>
        {
            var row = Assert.Single(GetTopLevelRows(cut));
            Assert.Contains("Kunde 00044", row.TextContent);
            Assert.NotEmpty(row.QuerySelectorAll("span.bg-yellow-200"));
        }, TimeSpan.FromSeconds(5));
    }

    private static IElement[] GetTopLevelRows(IRenderedComponent<AccountGrid> cut, bool includeChildRows = false)
    {
        return cut.Find("div.kal-data-grid > table > tbody").Children
            .Where(row => includeChildRows || !row.ClassList.Contains("kal-data-grid-child-row"))
            .ToArray();
    }
}
