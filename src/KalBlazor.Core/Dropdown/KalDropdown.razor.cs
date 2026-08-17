using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace SoftwareThingies.KalBlazor.Core;

public partial class KalDropdown
{
    protected override string ComponentClass => "kal-dropdown";

    protected override string DefaultClass => "relative inline-block text-left";

    private bool IsOpen { get; set; }

    [Parameter]
    public RenderFragment? TriggerTemplate { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public string TriggerText { get; set; } = "Open";

    [Parameter]
    public string AriaLabel { get; set; } = "Dropdown";

    [Parameter]
    public string? Icon { get; set; }

    /// <summary>
    /// Overrides the indicator icon while the dropdown panel is open.
    /// </summary>
    [Parameter]
    public string? OpenIcon { get; set; }

    /// <summary>
    /// Overrides the indicator icon while the dropdown panel is closed.
    /// </summary>
    [Parameter]
    public string? ClosedIcon { get; set; }

    [Parameter]
    public string IconClass { get; set; } = "size-4 shrink-0 text-slate-400";

    [Parameter]
    public bool ShowIcon { get; set; } = true;

    [Parameter]
    public RenderFragment<bool>? IconTemplate { get; set; }

    [Parameter]
    public string PanelClass { get; set; } = "absolute left-0 z-50 mt-2 min-w-full";

    private string TriggerCssClass =>
        "inline-flex items-center gap-2 whitespace-nowrap";

    private string EffectiveIcon =>
        Icon
        ?? (IsOpen
            ? OpenIcon ?? Icons.AngleUp
            : ClosedIcon ?? Icons.AngleDown);

    private void Toggle() => IsOpen = !IsOpen;

    private void CloseOnFocusOut(FocusEventArgs args) => IsOpen = false;
}
