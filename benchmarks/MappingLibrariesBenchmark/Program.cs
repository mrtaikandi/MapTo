using BenchmarkDotNet.Running;
using MappingLibrariesBenchmark.Scenarios;

var config = DefaultConfig.Instance.WithOptions(ConfigOptions.DisableOptimizationsValidator);

// BenchmarkRunner.Run<BenchmarkMappingLibraries>(config);
BenchmarkRunner.Run<BenchmarkMapToAndMapperly>(config);