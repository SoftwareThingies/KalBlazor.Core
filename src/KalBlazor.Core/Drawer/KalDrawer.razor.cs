using Microsoft.AspNetCore.Components;

namespace SoftwareThingies.KalBlazor.Core;

public partial class KalDrawer : IDisposable
{
    private CancellationTokenSource? _hoverCancellationTokenSource;
    private CancellationTokenSource? _mouseLeaveCancellationTokenSource;
    private bool _maximizedByHover;
    private bool _hasReceivedInitialState;
    private bool _isApplyingParameters;
    private KalDrawerState _lastReceivedInitialState;
    private KalDrawerState? _lastNotifiedState;

    protected override string ComponentClass =>
        $"kal-drawer {(IsMinimized ? "kal-drawer-minimized" : IsOpen ? "kal-drawer-open" : "kal-drawer-closed")}";

    protected override string DefaultClass => "fixed flex w-screen flex-col overflow-x-hidden bg-white text-slate-950 transition-[width,max-width,transform,translate] duration-200 ease-out";

    [Parameter]
    public RenderFragment<KalDrawerStateContext> ChildContent { get; set; } = default!;

    [Parameter]
    public string? Id { get; set; }

    [Parameter]
    public string? Name { get; set; }

    [Parameter]
    public KalDrawerSide Side { get; set; } = KalDrawerSide.Left;

    [Parameter]
    public DrawerWidth Width { get; set; } = DrawerWidth.Xs;

    [Parameter]
    public KalDrawerVariant Variant { get; set; } = KalDrawerVariant.Overlay;

    [Parameter]
    public KalDrawerBackdrop Backdrop { get; set; } = KalDrawerBackdrop.Darkened;

    [Parameter]
    public bool PreventClose { get; set; }

    [Parameter]
    public KalDrawerState InitialState { get; set; } = KalDrawerState.Maximized;

    [Parameter]
    public EventCallback<KalDrawerState> InitialStateChanged { get; set; }

    [Parameter]
    public EventCallback OnClosed { get; set; }

    [Parameter]
    public EventCallback OnOpened { get; set; }

    [Parameter]
    public bool Minimizable { get; set; }

    [Parameter]
    public bool ShowDefaultTrigger { get; set; } = true;

    [Parameter]
    public bool MinimizeOnClose { get; set; }

    [Parameter]
    public EventCallback<bool> MinimizeOnCloseChanged { get; set; }

    [Parameter]
    public bool ExpandOnHover { get; set; }

    [Parameter]
    public EventCallback<bool> ExpandOnHoverChanged { get; set; }

    [Parameter]
    public int ExpandOnHoverDelay { get; set; } = 500;

    [Parameter]
    public bool CollapseOnMouseLeave { get; set; } = true;

    [Parameter]
    public EventCallback<bool> CollapseOnMouseLeaveChanged { get; set; }

    [Parameter]
    public int CollapseOnMouseLeaveDelay { get; set; } = 500;

    [Parameter]
    public string MinimizedWidthClass { get; set; } = "!w-16 !max-w-16";

    [Parameter]
    public string MinimizedOffsetClass { get; set; } = "ml-16 mr-16 left-16 right-16";

    [Parameter]
    public string MinimizeIcon { get; set; } = Icons.AngleLeft;

    [Parameter]
    public string MaximizeIcon { get; set; } = Icons.Bars;

    [Parameter]
    public string MinimizeButtonAriaLabel { get; set; } = "Drawer minimieren";

    [Parameter]
    public string MaximizeButtonAriaLabel { get; set; } = "Drawer maximieren";

    [Parameter]
    public string MinimizeButtonClass { get; set; } = "m-2 inline-flex size-10 shrink-0 items-center justify-center self-end rounded hover:bg-slate-100";

    [Parameter]
    public string MinimizeIconClass { get; set; } = "text-xl transition-transform duration-200";

    [Parameter]
    public string MaximizeIconClass { get; set; } = "text-xl transition-transform duration-200";

    [Parameter]
    public RenderFragment? MinimizerTriggerTemplate { get; set; }

    [Parameter]
    public RenderFragment? MaximizerTriggerTemplate { get; set; }

    [CascadingParameter]
    internal KalDrawerContext? DrawerContext { get; set; }

    protected override string DynamicClass => $"{SideClass} {VerticalClass} {ZIndexClass} {WidthClass} {ShadowClass} {StateClass}".Trim();

    internal bool IsOpen => DrawerContext?.IsOpen(Key) == true;

    internal bool IsMinimized => DrawerContext?.IsMinimized(Key) == true;

    private bool IsVisible => IsOpen || IsMinimized;

    private bool IsHidden => !IsVisible;

    private KalDrawerStateContext CurrentStateContext =>
        new(CurrentState, Id, Name, Side, Variant);

    private KalDrawerState CurrentState =>
        DrawerContext is not null && TryGetKey(out var key)
            ? DrawerContext.GetState(key)
            : InitialState;

    private string Key => TryGetKey(out var key) ? key : string.Empty;

    private string SideClass => Side == KalDrawerSide.Right ? "right-0" : "left-0";

    private string VerticalClass
    {
        get
        {
            if (Variant != KalDrawerVariant.Clipped)
            {
                return "top-0 bottom-0";
            }

            return DrawerContext?.AppBarBottom == true
                ? "top-0 bottom-14"
                : "top-14 bottom-0";
        }
    }

    private string ZIndexClass => Variant == KalDrawerVariant.Clipped ? "z-40" : "z-50";

    private string ShadowClass => "shadow-xl";

    private string WidthClass =>
        IsMinimized
            ? MinimizedWidthClass
            : Width switch
            {
                DrawerWidth.Sm => "max-w-sm",
                DrawerWidth.Md => "max-w-md",
                DrawerWidth.Lg => "max-w-lg",
                DrawerWidth.Xl => "max-w-xl",
                DrawerWidth.TwoXl => "max-w-2xl",
                DrawerWidth.ThreeXl => "max-w-3xl",
                DrawerWidth.FourXl => "max-w-4xl",
                DrawerWidth.FiveXl => "max-w-5xl",
                DrawerWidth.SixXl => "max-w-6xl",
                DrawerWidth.SevenXl => "max-w-7xl",
                DrawerWidth.Full => "max-w-full",
                _ => "max-w-xs"
            };

    private string StateClass
    {
        get
        {
            if (IsVisible)
            {
                return "translate-x-0";
            }

            return Side == KalDrawerSide.Right ? "translate-x-full" : "-translate-x-full";
        }
    }

    private string MinimizeButtonCssClass =>
        $"{MinimizeButtonClass} {(IsMinimized ? "self-center" : string.Empty)}".Trim();

    private string EffectiveMinimizeButtonAriaLabel =>
        IsMinimized ? MaximizeButtonAriaLabel : MinimizeButtonAriaLabel;

    private string EffectiveButtonIcon => IsMinimized ? MaximizeIcon : MinimizeIcon;

    private string EffectiveButtonIconClass => IsMinimized ? MaximizeIconClass : MinimizeIconClass;

    private RenderFragment? EffectiveButtonContent =>
        IsMinimized
            ? MaximizerTriggerTemplate
            : MinimizerTriggerTemplate;

    protected override void OnInitialized()
    {
        if (DrawerContext is not null)
        {
            DrawerContext.StateChanged += StateHasChanged;
            DrawerContext.DrawerStateChanged += HandleDrawerStateChanged;
        }
    }

    protected override void OnParametersSet()
    {
        if (DrawerContext is not null && TryGetKey(out var key))
        {
            _isApplyingParameters = true;

            try
            {
                DrawerContext.Register(
                    key,
                    Side,
                    Width,
                    Variant,
                    Backdrop,
                    PreventClose,
                    Minimizable || MinimizeOnClose || InitialState == KalDrawerState.Minimized,
                    MinimizeOnClose,
                    MinimizedOffsetClass);

                if (!_hasReceivedInitialState || InitialState != _lastReceivedInitialState)
                {
                    DrawerContext.SetState(key, InitialState);
                    _lastReceivedInitialState = InitialState;
                    _hasReceivedInitialState = true;
                }

                _lastNotifiedState = DrawerContext.GetState(key);
            }
            finally
            {
                _isApplyingParameters = false;
            }
        }
    }

    private void HandleDrawerStateChanged(string changedKey)
    {
        if (_isApplyingParameters
            || DrawerContext is null
            || !TryGetKey(out var key)
            || !string.Equals(key, changedKey, StringComparison.Ordinal))
        {
            return;
        }

        var state = DrawerContext.GetState(key);

        if (state != KalDrawerState.Maximized)
        {
            _maximizedByHover = false;
        }

        if (_lastNotifiedState == state)
        {
            return;
        }

        var previousState = _lastNotifiedState;
        _lastNotifiedState = state;
        _ = InvokeAsync(async () =>
        {
            await InitialStateChanged.InvokeAsync(state);

            if (state == KalDrawerState.Hidden && previousState != KalDrawerState.Hidden)
            {
                await OnClosed.InvokeAsync();
            }

            if (state == KalDrawerState.Maximized && previousState != KalDrawerState.Maximized)
            {
                await OnOpened.InvokeAsync();
            }
        });
    }

    private void ToggleMinimized()
    {
        CancelHoverExpansion();
        CancelMouseLeaveCollapse();
        _maximizedByHover = false;

        if (DrawerContext is null || !TryGetKey(out var key))
        {
            return;
        }

        DrawerContext.ToggleMinimized(key);
    }

    internal void Minimize()
    {
        CancelHoverExpansion();
        CancelMouseLeaveCollapse();
        _maximizedByHover = false;

        if (DrawerContext is not null && TryGetKey(out var key))
        {
            DrawerContext.Minimize(key);
        }
    }

    internal void Maximize()
    {
        CancelHoverExpansion();
        CancelMouseLeaveCollapse();
        _maximizedByHover = false;

        if (DrawerContext is not null && TryGetKey(out var key))
        {
            DrawerContext.Maximize(key);
        }
    }

    private void HandleMouseEnter()
    {
        CancelHoverExpansion();
        CancelMouseLeaveCollapse();

        if (!ExpandOnHover
            || !IsMinimized
            || DrawerContext is null
            || !TryGetKey(out var key))
        {
            return;
        }

        var cancellationTokenSource = new CancellationTokenSource();
        _hoverCancellationTokenSource = cancellationTokenSource;
        _ = ExpandAfterHoverDelayAsync(key, cancellationTokenSource);
    }

    private async Task ExpandAfterHoverDelayAsync(
        string key,
        CancellationTokenSource cancellationTokenSource)
    {
        try
        {
            await Task.Delay(Math.Max(0, ExpandOnHoverDelay), cancellationTokenSource.Token);

            if (!cancellationTokenSource.IsCancellationRequested && IsMinimized)
            {
                await InvokeAsync(() =>
                {
                    if (DrawerContext is null || !IsMinimized)
                    {
                        return;
                    }

                    DrawerContext.Maximize(key);
                    _maximizedByHover = true;
                });
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (ReferenceEquals(_hoverCancellationTokenSource, cancellationTokenSource))
            {
                _hoverCancellationTokenSource = null;
            }

            cancellationTokenSource.Dispose();
        }
    }

    private void HandleMouseLeave()
    {
        CancelHoverExpansion();

        if (!CollapseOnMouseLeave
            || !_maximizedByHover
            || DrawerContext is null
            || !TryGetKey(out var key))
        {
            return;
        }

        var cancellationTokenSource = new CancellationTokenSource();
        _mouseLeaveCancellationTokenSource = cancellationTokenSource;
        _ = CollapseAfterMouseLeaveDelayAsync(key, cancellationTokenSource);
    }

    private async Task CollapseAfterMouseLeaveDelayAsync(
        string key,
        CancellationTokenSource cancellationTokenSource)
    {
        try
        {
            await Task.Delay(Math.Max(0, CollapseOnMouseLeaveDelay), cancellationTokenSource.Token);

            if (!cancellationTokenSource.IsCancellationRequested && _maximizedByHover)
            {
                await InvokeAsync(() =>
                {
                    if (DrawerContext is null || !_maximizedByHover)
                    {
                        return;
                    }

                    _maximizedByHover = false;
                    DrawerContext.Close(key);
                });
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            if (ReferenceEquals(_mouseLeaveCancellationTokenSource, cancellationTokenSource))
            {
                _mouseLeaveCancellationTokenSource = null;
            }

            cancellationTokenSource.Dispose();
        }
    }

    private void CancelHoverExpansion()
    {
        if (_hoverCancellationTokenSource is null)
        {
            return;
        }

        _hoverCancellationTokenSource.Cancel();
        _hoverCancellationTokenSource = null;
    }

    private void CancelMouseLeaveCollapse()
    {
        if (_mouseLeaveCancellationTokenSource is null)
        {
            return;
        }

        _mouseLeaveCancellationTokenSource.Cancel();
        _mouseLeaveCancellationTokenSource = null;
    }

    private bool TryGetKey(out string key)
    {
        if (!string.IsNullOrWhiteSpace(Id))
        {
            key = $"id:{Id}";
            return true;
        }

        if (!string.IsNullOrWhiteSpace(Name))
        {
            key = $"name:{Name}";
            return true;
        }

        key = string.Empty;
        return false;
    }

    public void Dispose()
    {
        CancelHoverExpansion();
        CancelMouseLeaveCollapse();

        if (DrawerContext is not null)
        {
            DrawerContext.StateChanged -= StateHasChanged;
            DrawerContext.DrawerStateChanged -= HandleDrawerStateChanged;

            if (TryGetKey(out var key))
            {
                DrawerContext.Unregister(key);
            }
        }
    }
}
