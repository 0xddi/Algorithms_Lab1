using System;
using System.Collections.Generic;
using System.Linq;

namespace Algorithms.Core;

/// <summary>
/// Хранит данные об одном алгоритме и его результатах.
/// </summary>
public class BenchmarkTask
{
    public string Name { get; }

    // Универсальная обертка: принимает срез данных double[], возвращает время (мс)
    public Func<double[], double> RunMeasurement { get; }

    /// <summary>
    /// Набор размеров N, на которых нужно прогнать эту задачу.
    /// Если не задан — по умолчанию 1..2000 (старое поведение).
    /// </summary>
    public int[] Sizes { get; }

    public List<(int N, double TimeMs, double TimeTicks)> Results { get; }
        = new List<(int, double, double)>(256);

    public BenchmarkTask(
        string name,
        Func<double[], double> runMeasurement,
        int[]? sizes = null)
    {
        Name = name;
        RunMeasurement = runMeasurement;
        Sizes = sizes ?? Enumerable.Range(1, 2000).ToArray();
    }
}