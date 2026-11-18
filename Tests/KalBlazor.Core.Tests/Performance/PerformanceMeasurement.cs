using System.Diagnostics;
using Xunit.Abstractions;

namespace SoftwareThingies.KalBlazor.Core.Tests.Performance;

internal sealed record MeasurementResult(string Name, int Iterations, double MedianMs, double MeanMs, double MaxMs);

internal static class PerformanceMeasurement
{
    /// <summary>
    /// Runs <paramref name="action" /> after a warm-up and reports timings to the test output.
    /// <paramref name="beforeEach" /> runs before every iteration without being measured.
    /// </summary>
    public static async Task<MeasurementResult> MeasureAsync(
        ITestOutputHelper output,
        string name,
        int iterations,
        Func<Task> action,
        int warmupIterations = 2,
        Func<Task>? beforeEach = null)
    {
        for (var index = 0; index < warmupIterations; index++)
        {
            await (beforeEach?.Invoke() ?? Task.CompletedTask);
            await action();
        }

        var timings = new double[iterations];

        for (var index = 0; index < iterations; index++)
        {
            await (beforeEach?.Invoke() ?? Task.CompletedTask);
            var stopwatch = Stopwatch.StartNew();
            await action();
            stopwatch.Stop();
            timings[index] = stopwatch.Elapsed.TotalMilliseconds;
        }

        Array.Sort(timings);
        var result = new MeasurementResult(name, iterations, timings[iterations / 2], timings.Average(), timings[^1]);

        output.WriteLine($"{result.Name}: Median {result.MedianMs:F2} ms | Mittel {result.MeanMs:F2} ms | Max {result.MaxMs:F2} ms ({iterations} Durchläufe)");
        return result;
    }
}
