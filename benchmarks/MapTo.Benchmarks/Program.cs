using BenchmarkDotNet.Running;
using MapTo.Benchmarks.Scenarios;

var config = DefaultConfig.Instance
    .WithOptions(ConfigOptions.DisableOptimizationsValidator)
    .WithArtifactsPath(@"bin\artifacts");

// BenchmarkRunner.Run<BenchmarkMappingLibraries>(config);
BenchmarkRunner.Run<BenchmarkMapToAndMapperly>(config);