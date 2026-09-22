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
    public Func<double[], double> RunMeasurement { get; }
    
    public bool IsStepMeasurement { get; set; }
    
    public List<(int N, double TimeMs, double TimeTicks)> Results { get; } 
        = new List<(int, double, double)>(2000);

    public BenchmarkTask(string name, Func<double[], double> runMeasurement, bool isStepMeasurement = false)
    {
        Name = name;
        RunMeasurement = runMeasurement;
    }
}