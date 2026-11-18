using Bunit;
using SoftwareThingies.KalBlazor.Core.Tests.TestComponents;

namespace SoftwareThingies.KalBlazor.Core.Tests.DataGrid;

/// <summary>
/// Filter results are cached across re-renders, so these tests ensure item changes are still picked up.
/// </summary>
public sealed class DataGridItemChangeTests : BunitContext
{
    public DataGridItemChangeTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void ItemAddedToSameList_IsShownAfterParentReRender()
    {
        var items = new List<MutableItem> { new("Alpha"), new("Beta") };
        var cut = Render<MutableGrid>(parameters => parameters
            .Add(grid => grid.Items, items)
            .Add(grid => grid.FilterText, "Alp"));

        items.Add(new MutableItem("Alpaka"));
        cut.Render();

        Assert.Equal(["Alpha", "Alpaka"], GetItemRowTexts(cut));
    }

    [Fact]
    public void ItemReplacedInSameList_IsReEvaluated()
    {
        var items = new List<MutableItem> { new("Alpha"), new("Beta") };
        var cut = Render<MutableGrid>(parameters => parameters
            .Add(grid => grid.Items, items)
            .Add(grid => grid.FilterText, "Alp"));

        items[0] = new MutableItem("Gamma");
        cut.Render();

        Assert.Empty(GetItemRowTexts(cut));
    }

    [Fact]
    public void NewListWithSameItems_KeepsResult()
    {
        var items = new List<MutableItem> { new("Alpha"), new("Beta") };
        var cut = Render<MutableGrid>(parameters => parameters
            .Add(grid => grid.Items, items)
            .Add(grid => grid.FilterText, "Beta"));

        cut.Render(parameters => parameters.Add(grid => grid.Items, items.ToList()));

        Assert.Equal(["Beta"], GetItemRowTexts(cut));
    }

    [Fact]
    public async Task PropertyChangedInPlace_IsAppliedByRefreshAsync()
    {
        var items = new List<MutableItem> { new("Alpha"), new("Beta") };
        var cut = Render<MutableGrid>(parameters => parameters
            .Add(grid => grid.Items, items)
            .Add(grid => grid.FilterText, "Alp"));

        items[1].Name = "Alpenrose";
        await cut.InvokeAsync(() => cut.Instance.Grid!.RefreshAsync());

        Assert.Equal(["Alpha", "Alpenrose"], GetItemRowTexts(cut));
    }

    [Fact]
    public async Task ChildPropertyChangedInPlace_IsAppliedByRefreshAsync()
    {
        var parent = new MutableItem("Eltern");
        parent.Children.Add(new MutableItem("Kind"));
        var cut = Render<MutableGrid>(parameters => parameters
            .Add(grid => grid.Items, new List<MutableItem> { parent, new("Andere") })
            .Add(grid => grid.FilterText, "Treffer"));
        Assert.Empty(GetItemRowTexts(cut));

        parent.Children[0].Name = "Treffer";
        await cut.InvokeAsync(() => cut.Instance.Grid!.RefreshAsync());

        Assert.Equal(["Eltern"], GetItemRowTexts(cut));
    }

    [Fact]
    public void ChangedFilterTextParameter_IsApplied()
    {
        var parent = new MutableItem("Eltern");
        parent.Children.Add(new MutableItem("Kind"));
        var items = new List<MutableItem> { new("Alpha"), new("Beta"), parent };
        var cut = Render<MutableGrid>(parameters => parameters
            .Add(grid => grid.Items, items)
            .Add(grid => grid.FilterText, "Alp"));

        cut.Render(parameters => parameters.Add(grid => grid.FilterText, "Beta"));
        Assert.Equal(["Beta"], GetItemRowTexts(cut));

        cut.Render(parameters => parameters.Add(grid => grid.FilterText, "Kind"));
        Assert.Equal(["Eltern"], GetItemRowTexts(cut));

        cut.Render(parameters => parameters.Add(grid => grid.FilterText, (string?)null));
        Assert.Equal(["Alpha", "Beta", "Eltern"], GetItemRowTexts(cut));
    }

    private static string[] GetItemRowTexts(IRenderedComponent<MutableGrid> cut)
    {
        return cut.Find("div.kal-data-grid > table > tbody").Children
            .Where(row => !row.ClassList.Contains("kal-data-grid-child-row"))
            .Select(row => row.TextContent.Trim())
            .ToArray();
    }
}
