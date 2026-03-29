```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8037/25H2/2025Update/HudsonValley2)
13th Gen Intel Core i7-1360P 2.20GHz, 1 CPU, 16 logical and 12 physical cores
.NET SDK 9.0.307
  [Host]     : .NET 9.0.11 (9.0.11, 9.0.1125.51716), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 9.0.11 (9.0.11, 9.0.1125.51716), X64 RyuJIT x86-64-v3


```
| Method                        | Mean     | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------ |---------:|---------:|---------:|------:|--------:|----------:|------------:|
| Method_DictInside_Or          | 22.74 μs | 0.448 μs | 0.440 μs |  1.00 |    0.03 |     248 B |        1.00 |
| Method_DictOutside_Or         | 23.90 μs | 0.474 μs | 0.616 μs |  1.05 |    0.03 |      64 B |        0.26 |
| Method_DictOutside_SeparateIf | 23.28 μs | 0.396 μs | 0.351 μs |  1.02 |    0.02 |      64 B |        0.26 |
