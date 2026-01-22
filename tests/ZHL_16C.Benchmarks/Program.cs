using BenchmarkDotNet.Running;
using ZHL_16C.Benchmarks;

ValidationTests.Run_40m_45min_Benchmark();

BenchmarkRunner.Run<Zhl16CBenchmarks>();