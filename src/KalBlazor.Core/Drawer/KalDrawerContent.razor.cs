using Microsoft.AspNetCore.Components;

namespace SoftwareThingies.KalBlazor.Core;

public partial class KalDrawerContent
{
    protected override string ComponentClass => "kal-drawer-content";

    protected override string DefaultClass => "min-h-0 flex-1 overflow-y-auto";

    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
