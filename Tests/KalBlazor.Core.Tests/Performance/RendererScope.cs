using Microsoft.Extensions.DependencyInjection;

namespace SoftwareThingies.KalBlazor.Core.Tests.Performance;

/// <summary>
/// Owns a <see cref="MeasuringRenderer" /> together with the service provider it renders with.
/// </summary>
internal sealed class RendererScope : IAsyncDisposable
{
    private readonly ServiceProvider _services;

    private RendererScope(ServiceProvider services)
    {
        _services = services;
        Renderer = new MeasuringRenderer(services);
    }

    public MeasuringRenderer Renderer { get; }

    public static async Task<RendererScope> RenderAsync<TComponent>(params object[] services)
        where TComponent : Microsoft.AspNetCore.Components.IComponent
    {
        var serviceCollection = new ServiceCollection();

        foreach (var service in services)
        {
            serviceCollection.AddSingleton(service.GetType(), service);
        }

        var scope = new RendererScope(serviceCollection.BuildServiceProvider());
        await scope.Renderer.RenderAsync<TComponent>();
        scope.Renderer.ResetStatistics();
        return scope;
    }

    public async ValueTask DisposeAsync()
    {
        await Renderer.DisposeAsync();
        await _services.DisposeAsync();
    }
}
