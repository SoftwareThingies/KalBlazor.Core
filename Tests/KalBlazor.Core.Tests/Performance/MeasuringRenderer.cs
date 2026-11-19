using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging.Abstractions;

namespace SoftwareThingies.KalBlazor.Core.Tests.Performance;

/// <summary>
/// Minimal Blazor renderer that measures only rendering and diffing, like the work Blazor does in the browser before
/// applying DOM changes. Unlike bUnit it doesn't regenerate the markup of the whole component tree after each render,
/// which otherwise dominates timings for large grids.
/// </summary>
internal sealed class MeasuringRenderer(IServiceProvider services) : Renderer(services, NullLoggerFactory.Instance)
{
    private readonly Dictionary<string, ulong> _clickHandlersByClass = new(StringComparer.Ordinal);

    public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

    /// <summary>
    /// Number of components rendered since the last <see cref="ResetStatistics" />.
    /// </summary>
    public int RenderedComponents { get; private set; }

    /// <summary>
    /// Number of DOM edits Blazor would send to the browser since the last <see cref="ResetStatistics" />.
    /// </summary>
    public int DomEdits { get; private set; }

    public Task RenderAsync<TComponent>() where TComponent : IComponent
    {
        return Dispatcher.InvokeAsync(async () =>
        {
            var componentId = AssignRootComponentId(InstantiateComponent(typeof(TComponent)));
            await RenderRootComponentAsync(componentId);
        });
    }

    /// <summary>
    /// Clicks the element carrying <paramref name="cssClass" />, e.g. a drawer trigger.
    /// </summary>
    public Task ClickAsync(string cssClass)
    {
        var handlerId = _clickHandlersByClass[cssClass];
        return Dispatcher.InvokeAsync(() => DispatchEventAsync(handlerId, null, new MouseEventArgs()));
    }

    public void ResetStatistics()
    {
        RenderedComponents = 0;
        DomEdits = 0;
    }

    protected override void HandleException(Exception exception)
    {
        throw exception;
    }

    protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
    {
        RenderedComponents += renderBatch.UpdatedComponents.Count;

        for (var index = 0; index < renderBatch.UpdatedComponents.Count; index++)
        {
            DomEdits += renderBatch.UpdatedComponents.Array[index].Edits.Count;
        }

        CollectClickHandlers(renderBatch.ReferenceFrames);
        return Task.CompletedTask;
    }

    private void CollectClickHandlers(ArrayRange<RenderTreeFrame> frames)
    {
        string? currentClass = null;

        for (var index = 0; index < frames.Count; index++)
        {
            ref var frame = ref frames.Array[index];

            switch (frame.FrameType)
            {
                case RenderTreeFrameType.Element:
                    currentClass = null;
                    break;
                case RenderTreeFrameType.Attribute when frame.AttributeName == "class":
                    currentClass = frame.AttributeValue as string;
                    break;
                case RenderTreeFrameType.Attribute when frame.AttributeName == "onclick" && frame.AttributeEventHandlerId != 0 && currentClass is not null:
                    foreach (var cssClass in currentClass.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    {
                        _clickHandlersByClass[cssClass] = frame.AttributeEventHandlerId;
                    }

                    break;
            }
        }
    }
}
