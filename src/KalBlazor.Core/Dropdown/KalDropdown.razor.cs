using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace SoftwareThingies.KalBlazor.Core;

public partial class KalDropdown
{
    private IJSObjectReference? _module;
    private bool _wasOpen;

    protected override string ComponentClass => "kal-dropdown";

    protected override string DefaultClass => "relative inline-block text-left";

    private bool IsOpen { get; set; }

    private ElementReference TriggerElement { get; set; }

    private ElementReference PanelElement { get; set; }

    private ElementReference RootElement { get; set; }

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

    /// <summary>
    /// Runs when the dropdown trigger is clicked. The open state is changed only
    /// after this callback has completed.
    /// </summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    [Parameter]
    public string PanelClass { get; set; } = "absolute left-0 z-50 mt-2 min-w-full";

    private string TriggerCssClass =>
        "inline-flex items-center gap-2 whitespace-nowrap";

    private string EffectiveIcon =>
        Icon
        ?? (IsOpen
            ? OpenIcon ?? Icons.AngleUp
            : ClosedIcon ?? Icons.AngleDown);

    private async Task HandleTriggerClick(MouseEventArgs args)
    {
        await OnClick.InvokeAsync(args);
        IsOpen = !IsOpen;
    }

    private void CloseAfterPanelClick()
    {
        // This handler runs after handlers on clicked descendants have run.
        // Keeping the close here prevents focusout from removing the clicked
        // element before its native/Blazor click is delivered.
        IsOpen = false;
    }

    private async Task CloseOnFocusOut(FocusEventArgs args)
    {
        // focusout happens before click. Wait until the browser has completed the
        // focus change so a click into the panel cannot remove its target first.
        if (_module is not null)
        {
            await _module.InvokeVoidAsync("waitForFocusChange");

            if (await _module.InvokeAsync<bool>("containsFocusedElement", RootElement))
            {
                return;
            }
        }

        IsOpen = false;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (IsOpen)
        {
            _module ??= await JsRuntime.InvokeAsync<IJSObjectReference>(
                "import",
                "./_content/SoftwareThingies.KalBlazor.Core/Dropdown/KalDropdown.razor.js");

            await _module.InvokeVoidAsync("positionKalDropdownPanel", PanelElement, TriggerElement);
            _wasOpen = true;
        }
        else if (_wasOpen && _module is not null)
        {
            await _module.InvokeVoidAsync("disposeKalDropdownPanel", PanelElement);
            _wasOpen = false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try
            {
                await _module.InvokeVoidAsync("disposeKalDropdownPanel", PanelElement);
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }
    }
}
