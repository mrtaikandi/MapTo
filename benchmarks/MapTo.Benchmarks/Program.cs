using BenchmarkDotNet.Running;
using MapTo.Benchmarks.Scenarios;

var config = DefaultConfig.Instance.WithArtifactsPath(@"bin\artifacts");

// BenchmarkRunner.Run<BenchmarkMappingLibraries>(config);
BenchmarkRunner.Run<BenchmarkMapToAndMapperly>(config);