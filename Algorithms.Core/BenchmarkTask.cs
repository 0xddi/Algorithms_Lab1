using System;
using System.Collections.Generic;

namespace Algorithms.Core;

/// <summary>
/// Хранит данные об одном алгоритме и его результатах.
/// </summary>
public class BenchmarkTask
{
    public string Name { get; }
    
    // Универсальная обертка: принимает срез данных double[], возвращает время (мс)
    public Func<double[], (double TimeMs, long? Steps)> RunMeasurement { get; }

    public List<(int N, double TimeMs, long? Steps)> Results { get; } = new();

    public BenchmarkTask(string name, Func<double[], (double TimeMs, long? Steps)> runMeasurement)
    {
        Name = name;
        RunMeasurement = runMeasurement;
    }
}