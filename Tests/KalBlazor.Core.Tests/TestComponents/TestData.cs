namespace SoftwareThingies.KalBlazor.Core.Tests.TestComponents;

public sealed record Account(int Id, string Name, string Segment, string Status, string Owner, decimal Revenue, IReadOnlyList<Invoice> Invoices);

public sealed record Invoice(string Number, string Period, decimal Amount, string Status);

public static class TestData
{
    private static readonly string[] Segments = ["Enterprise", "Mittelstand", "Startup", "Public"];
    private static readonly string[] Statuses = ["Aktiv", "Pausiert", "Gekündigt"];
    private static readonly string[] Owners = ["Anna", "Ben", "Clara", "David", "Eva"];

    /// <summary>
    /// Creates accounts named "Kunde 00001" ... The single account matching <paramref name="uniqueMarkerEvery" />
    /// gets the segment "Zielgruppe" so filters can narrow a large data set to a few rows.
    /// </summary>
    public static List<Account> CreateAccounts(int count, int invoicesPerAccount = 3, int uniqueMarkerEvery = 500)
    {
        return Enumerable.Range(1, count)
            .Select(index => new Account(
                index,
                $"Kunde {index:D5}",
                index % uniqueMarkerEvery == 0 ? "Zielgruppe" : Segments[index % Segments.Length],
                Statuses[index % Statuses.Length],
                Owners[index % Owners.Length],
                index * 13.37m,
                Enumerable.Range(1, invoicesPerAccount)
                    .Select(invoice => new Invoice($"RE-{index:D5}-{invoice}", $"2026-{invoice:D2}", invoice * 100m, "Offen"))
                    .ToArray()))
            .ToList();
    }
}

/// <summary>
/// Counts how often grid cells were rendered, which reveals unnecessary grid re-renders.
/// </summary>
public sealed class RenderCounter
{
    public int CellRenders { get; private set; }

    public void CountCell() => CellRenders++;

    public void Reset() => CellRenders = 0;
}

/// <summary>
/// Supplies the page with data without component parameters, mirroring a routed page whose
/// parameters do not change when a layout re-renders.
/// </summary>
public sealed class GridPageState
{
    public List<Account> Items { get; set; } = [];

    public string? FilterText { get; set; }

    public bool WithChildRows { get; set; }

    public bool BindDrawerState { get; set; }
}

public sealed class MutableItem(string name)
{
    public string Name { get; set; } = name;

    public List<MutableItem> Children { get; } = [];
}
