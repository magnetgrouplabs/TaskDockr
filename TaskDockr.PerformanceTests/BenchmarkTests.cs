using System;
using BenchmarkDotNet.Attributes;
using TaskDockr; // reference to main project

namespace TaskDockr.PerformanceTests;

[MemoryDiagnoser]
public class StartupBenchmark
{
    [Benchmark]
    public void Startup_Performance()
    {
        // Arrange & Act
        var app = new Program(); // assuming Program class bootstraps the app
        // No explicit return needed; just measuring construction
    }
}