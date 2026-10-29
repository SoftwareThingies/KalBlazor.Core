using Microsoft.AspNetCore.Components;

namespace SoftwareThingies.KalBlazor.Core;

public partial class KalToggle
{
    protected override string ComponentClass => "kal-toggle";

    protected override string DefaultClass => "inline-flex items-center gap-3";

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public string AriaLabel { get; set; } = "Umschalten";

    [Parameter]
    public bool IsToggled { get; set; }

    [Parameter]
    public EventCallback<bool> IsToggledChanged { get; set; }

    [Parameter]
    public EventCallback<bool> OnToggle { get; set; }

    [Parameter]
    public EventCallback<bool> OnToggled { get; set; }

    [Parameter]
    public EventCallback<bool> OnToggleAsync { get; set; }

    [Parameter]
    public EventCallback<bool> OnToggledAsync { get; set; }

    [Parameter]
    public string InactiveCss { get; set; } = "kal-toggle-track relative inline-flex h-7 w-12 items-center rounded-full bg-slate-300 transition-colors duration-200 dark:bg-slate-700";

    [Parameter]
    public string ActiveCss { get; set; } = "kal-toggle-track relative inline-flex h-7 w-12 items-center rounded-full bg-sky-600 transition-colors duration-200 dark:bg-sky-500";

    [Parameter]
    public string InactiveThumbCss { get; set; } = "kal-toggle-thumb inline-block h-5 w-5 translate-x-1 rounded-full bg-white shadow-sm transition-transform duration-200";

    [Parameter]
    public string ActiveThumbCss { get; set; } = "kal-toggle-thumb inline-block h-5 w-5 translate-x-6 rounded-full bg-white shadow-sm transition-transform duration-200";

    private string IsToggledText => IsToggled ? "true" : "false";

    private async Task ToggleAsync()
    {
        var newValue = !IsToggled;

        await OnToggle.InvokeAsync(newValue);
        await OnToggleAsync.InvokeAsync(newValue);

        IsToggled = newValue;
        await IsToggledChanged.InvokeAsync(newValue);

        await OnToggled.InvokeAsync(newValue);
        await OnToggledAsync.InvokeAsync(newValue);
    }
}
